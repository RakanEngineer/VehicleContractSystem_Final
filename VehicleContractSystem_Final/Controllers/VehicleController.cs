using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public VehicleController(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var query = await BuildAccessibleVehiclesQueryAsync();
            var vehicles = await query
                .OrderBy(v => v.LicenseNumber)
                .ToListAsync();

            return View(vehicles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var query = await BuildAccessibleVehiclesQueryAsync();
            var vehicle = await query.FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        private async Task<IQueryable<Vehicle>> BuildAccessibleVehiclesQueryAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            var query = _context.Vehicles
                .AsNoTracking()
                .Include(v => v.VehicleType)
                .Include(v => v.VehicleModel)
                .Include(v => v.Color)
                .Include(v => v.Owner)
                .Include(v => v.Contracts)
                    .ThenInclude(c => c.Office)
                .AsQueryable();

            if (currentUser == null)
                return query.Where(v => false);

            if (await _currentUserService.IsSuperAdminAsync())
                return query;

            if (await _currentUserService.IsOfficeAdminAsync())
                return query.Where(v => v.Contracts.Any(c => c.OfficeId == currentUser.OfficeId));

            return query.Where(v => v.Contracts.Any(c => c.CreatedByUserId == currentUser.Id));
        }
    }
}
