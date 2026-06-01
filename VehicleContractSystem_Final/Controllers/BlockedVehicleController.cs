using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize(Roles = "SuperAdmin,OfficeAdmin")]
    public class BlockedVehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public BlockedVehicleController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? search, bool showInactive = false)
        {
            var query = _context.BlockedVehicles
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .AsQueryable();

            if (!showInactive)
                query = query.Where(b => b.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                query = query.Where(b =>
                    (b.ChassisNumber != null && b.ChassisNumber.Contains(normalizedSearch)) ||
                    (b.LicenseNumber != null && b.LicenseNumber.Contains(normalizedSearch)) ||
                    (b.PlateCode != null && b.PlateCode.Contains(normalizedSearch)) ||
                    b.Reason.Contains(normalizedSearch) ||
                    b.Source.Contains(normalizedSearch));
            }

            ViewBag.Search = search;
            ViewBag.ShowInactive = showInactive;

            return View(await query.Take(300).ToListAsync());
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            return View(new BlockedVehicle());
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlockedVehicle blockedVehicle)
        {
            NormalizeBlockedVehicle(blockedVehicle);

            if (string.IsNullOrWhiteSpace(blockedVehicle.ChassisNumber) &&
                string.IsNullOrWhiteSpace(blockedVehicle.LicenseNumber))
            {
                ModelState.AddModelError(string.Empty, "يرجى إدخال رقم الشاصي أو رقم المركبة");
            }

            if (await HasDuplicateActiveBlockedVehicleAsync(blockedVehicle))
            {
                ModelState.AddModelError(string.Empty, "يوجد سجل منع نشط مسبقاً لنفس رقم الشاصي أو رقم المركبة");
            }

            if (!ModelState.IsValid)
                return View(blockedVehicle);

            blockedVehicle.IsActive = true;
            blockedVehicle.CreatedAt = DateTime.Now;
            blockedVehicle.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _context.BlockedVehicles.Add(blockedVehicle);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "إضافة مركبة ممنوعة",
                "BlockedVehicle",
                blockedVehicle.Id.ToString(),
                $"تمت إضافة مركبة ممنوعة. الشاصي: {ValueOrDash(blockedVehicle.ChassisNumber)}، الرقم: {ValueOrDash(blockedVehicle.LicenseNumber)}");

            TempData["SuccessMessage"] = "تمت إضافة المركبة إلى قائمة المنع";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? deactivationReason)
        {
            var blockedVehicle = await _context.BlockedVehicles.FindAsync(id);
            if (blockedVehicle == null)
                return NotFound();

            if (!blockedVehicle.IsActive)
            {
                TempData["InfoMessage"] = "سجل المنع غير نشط مسبقاً";
                return RedirectToAction(nameof(Index), new { showInactive = true });
            }

            blockedVehicle.IsActive = false;
            blockedVehicle.DeactivatedAt = DateTime.Now;
            blockedVehicle.DeactivatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            blockedVehicle.DeactivationReason = string.IsNullOrWhiteSpace(deactivationReason)
                ? "تعطيل يدوي"
                : deactivationReason.Trim();
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "تعطيل منع مركبة",
                "BlockedVehicle",
                blockedVehicle.Id.ToString(),
                $"تم تعطيل منع المركبة. الشاصي: {ValueOrDash(blockedVehicle.ChassisNumber)}، الرقم: {ValueOrDash(blockedVehicle.LicenseNumber)}، سبب التعطيل: {blockedVehicle.DeactivationReason}");

            TempData["SuccessMessage"] = "تم تعطيل سجل المنع";
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizeBlockedVehicle(BlockedVehicle blockedVehicle)
        {
            blockedVehicle.ChassisNumber = NormalizeOptional(blockedVehicle.ChassisNumber);
            blockedVehicle.LicenseNumber = NormalizeOptional(blockedVehicle.LicenseNumber);
            blockedVehicle.PlateCode = NormalizeOptional(blockedVehicle.PlateCode);
            blockedVehicle.Reason = (blockedVehicle.Reason ?? string.Empty).Trim();
            blockedVehicle.Source = string.IsNullOrWhiteSpace(blockedVehicle.Source)
                ? "إدخال يدوي"
                : blockedVehicle.Source.Trim();
        }

        private async Task<bool> HasDuplicateActiveBlockedVehicleAsync(BlockedVehicle blockedVehicle)
        {
            return await _context.BlockedVehicles.AnyAsync(b =>
                b.IsActive &&
                ((!string.IsNullOrWhiteSpace(blockedVehicle.ChassisNumber) && b.ChassisNumber == blockedVehicle.ChassisNumber) ||
                 (!string.IsNullOrWhiteSpace(blockedVehicle.LicenseNumber) && b.LicenseNumber == blockedVehicle.LicenseNumber)));
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ValueOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}
