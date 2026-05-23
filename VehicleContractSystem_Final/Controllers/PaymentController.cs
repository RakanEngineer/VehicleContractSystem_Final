using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public PaymentController(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Contract");
        }

        [HttpGet]
        public async Task<IActionResult> Add(int contractId)
        {
            var contract = await GetContractWithPaymentsAsync(contractId);
            if (contract == null)
                return NotFound();

            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية إضافة دفعة لهذا العقد";
                return RedirectToAction("Index", "Contract");
            }

            ViewBag.Contract = contract;
            return View(new Payment
            {
                ContractId = contract.Id,
                PaymentDate = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Payment payment)
        {
            var contract = await GetContractWithPaymentsAsync(payment.ContractId);
            if (contract == null)
                return NotFound();

            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية إضافة دفعة لهذا العقد";
                return RedirectToAction("Index", "Contract");
            }

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Challenge();

            ModelState.Remove(nameof(Payment.Contract));
            ModelState.Remove(nameof(Payment.RecordedBy));
            ModelState.Remove(nameof(Payment.RecordedByUserId));

            if (payment.Amount <= 0)
                ModelState.AddModelError(nameof(Payment.Amount), "يجب أن يكون مبلغ الدفعة أكبر من صفر");

            if (payment.Amount > contract.RemainingAmount)
                ModelState.AddModelError(nameof(Payment.Amount), "لا يمكن أن يتجاوز مبلغ الدفعة المبلغ المتبقي");

            if (!ModelState.IsValid)
            {
                ViewBag.Contract = contract;
                return View(payment);
            }

            payment.Contract = contract;
            payment.RecordedBy = currentUser;
            payment.RecordedByUserId = currentUser.Id;
            payment.CreatedAt = DateTime.Now;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إضافة الدفعة بنجاح";
            return RedirectToAction("Details", "Contract", new { id = contract.Id });
        }

        private async Task<Contract?> GetContractWithPaymentsAsync(int contractId)
        {
            return await _context.Contracts
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == contractId);
        }
    }
}
