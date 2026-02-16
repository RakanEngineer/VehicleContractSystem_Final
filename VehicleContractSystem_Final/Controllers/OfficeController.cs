using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Models.ViewModel;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class OfficeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfficeController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: Office
        public async Task<IActionResult> Index()
        {
            var offices = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .ToListAsync();

            return View(offices);
        }

        // GET: Office/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var office = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (office == null)
                return NotFound();

            var viewModel = new OfficeViewModel
            {
                Office = office,
                Staff = office.Staff.ToList(),
                TotalContracts = office.Contracts.Count,
                OpenContracts = office.Contracts.Count(c => c.Status == ContractStatus.Open),
                ClosedContracts = office.Contracts.Count(c => c.Status == ContractStatus.Closed)
            };

            return View(viewModel);
        }

        // GET: Office/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Office/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Office office)
        {
            if (ModelState.IsValid)
            {
                _context.Add(office);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم إنشاء المكتب بنجاح";
                return RedirectToAction(nameof(Index));
            }

            return View(office);
        }

        // GET: Office/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var office = await _context.Offices.FindAsync(id);
            if (office == null)
                return NotFound();

            return View(office);
        }

        // POST: Office/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Office office)
        {
            if (id != office.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(office);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم تحديث المكتب بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OfficeExists(office.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(office);
        }

        // GET: Office/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var office = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (office == null)
                return NotFound();

            if (office.Staff.Any() || office.Contracts.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن حذف مكتب يحتوي على موظفين أو عقود";
                return RedirectToAction(nameof(Details), new { id = office.Id });
            }

            return View(office);
        }

        // POST: Office/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var office = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (office == null)
                return NotFound();

            if (office.Staff.Any() || office.Contracts.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن حذف مكتب يحتوي على موظفين أو عقود";
                return RedirectToAction(nameof(Index));
            }

            _context.Offices.Remove(office);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف المكتب بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private bool OfficeExists(int id)
        {
            return _context.Offices.Any(e => e.Id == id);
        }
    }
}
