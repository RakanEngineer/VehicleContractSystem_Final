using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class OfficeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OfficeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var offices = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .ToListAsync();

            await ExcludeSuperAdminsFromStaffAsync(offices);

            return View(offices);
        }

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

            await ExcludeSuperAdminsFromStaffAsync(new[] { office });

            return View(office);
        }

        public IActionResult Create()
        {
            return View();
        }

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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var office = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (office == null)
                return NotFound();

            await ExcludeSuperAdminsFromStaffAsync(new[] { office });

            return View(office);
        }

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

                    throw;
                }
            }

            var officeWithRelations = await _context.Offices
                .Include(o => o.Staff)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(o => o.Id == office.Id) ?? office;
            await ExcludeSuperAdminsFromStaffAsync(new[] { officeWithRelations });

            return View(officeWithRelations);
        }

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

            await ExcludeSuperAdminsFromStaffAsync(new[] { office });

            if (office.Staff.Any() || office.Contracts.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن حذف مكتب يحتوي على موظفين أو عقود";
                return RedirectToAction(nameof(Details), new { id = office.Id });
            }

            return View(office);
        }

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

            await ExcludeSuperAdminsFromStaffAsync(new[] { office });

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

        private async Task ExcludeSuperAdminsFromStaffAsync(IEnumerable<Office> offices)
        {
            foreach (var office in offices)
            {
                var visibleStaff = new List<ApplicationUser>();
                foreach (var staff in office.Staff)
                {
                    if (!await _userManager.IsInRoleAsync(staff, "SuperAdmin"))
                        visibleStaff.Add(staff);
                }

                office.Staff = visibleStaff;
            }
        }
    }
}
