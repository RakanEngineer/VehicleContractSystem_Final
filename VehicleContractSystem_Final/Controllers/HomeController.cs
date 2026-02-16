using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Models.ViewModel;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _userManager = userManager;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Challenge();

            var viewModel = new DashboardViewModel
            {
                UserName = currentUser.FullName
            };

            // Super Admin Dashboard
            if (await _currentUserService.IsSuperAdminAsync())
            {
                viewModel.UserRole = "مدير النظام";
                viewModel.TotalContracts = await _context.Contracts.CountAsync();
                viewModel.OpenContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Open);
                viewModel.ClosedContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Closed);
                viewModel.TodayContracts = await _context.Contracts
                    .CountAsync(c => c.CreatedAt.Date == DateTime.Today);
                viewModel.TotalUsers = await _context.Users.CountAsync();
                viewModel.TotalOffices = await _context.Offices.CountAsync();

                viewModel.RecentContracts = await _context.Contracts
                    .Include(c => c.Office)
                    .Include(c => c.CreatedByUser)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Top performers across all offices
                viewModel.TopPerformers = await _context.Contracts
                    .GroupBy(c => new { c.CreatedByUserId, c.CreatedByUser!.FirstName, c.CreatedByUser.LastName })
                    .Select(g => new TopPerformer
                    {
                        UserName = g.Key.CreatedByUserId,
                        FullName = g.Key.FirstName + " " + g.Key.LastName,
                        ContractCount = g.Count()
                    })
                    .OrderByDescending(t => t.ContractCount)
                    .Take(5)
                    .ToListAsync();
            }
            // Office Admin Dashboard
            else if (await _currentUserService.IsOfficeAdminAsync())
            {
                viewModel.UserRole = "مدير المكتب";

                var office = await _context.Offices.FindAsync(currentUser.OfficeId);
                viewModel.OfficeName = office?.Name ?? "";

                viewModel.TotalContracts = await _context.Contracts
                    .CountAsync(c => c.OfficeId == currentUser.OfficeId);
                viewModel.OpenContracts = await _context.Contracts
                    .CountAsync(c => c.OfficeId == currentUser.OfficeId && c.Status == ContractStatus.Open);
                viewModel.ClosedContracts = await _context.Contracts
                    .CountAsync(c => c.OfficeId == currentUser.OfficeId && c.Status == ContractStatus.Closed);
                viewModel.TodayContracts = await _context.Contracts
                    .CountAsync(c => c.OfficeId == currentUser.OfficeId && c.CreatedAt.Date == DateTime.Today);
                viewModel.TotalUsers = await _context.Users
                    .CountAsync(u => u.OfficeId == currentUser.OfficeId);

                viewModel.RecentContracts = await _context.Contracts
                    .Include(c => c.CreatedByUser)
                    .Where(c => c.OfficeId == currentUser.OfficeId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Top performers in this office
                viewModel.TopPerformers = await _context.Contracts
                    .Where(c => c.OfficeId == currentUser.OfficeId)
                    .GroupBy(c => new { c.CreatedByUserId, c.CreatedByUser!.FirstName, c.CreatedByUser.LastName })
                    .Select(g => new TopPerformer
                    {
                        UserName = g.Key.CreatedByUserId,
                        FullName = g.Key.FirstName + " " + g.Key.LastName,
                        ContractCount = g.Count()
                    })
                    .OrderByDescending(t => t.ContractCount)
                    .Take(5)
                    .ToListAsync();
            }
            // User Dashboard
            else
            {
                viewModel.UserRole = "موظف";

                var office = await _context.Offices.FindAsync(currentUser.OfficeId);
                viewModel.OfficeName = office?.Name ?? "";

                viewModel.TotalContracts = await _context.Contracts
                    .CountAsync(c => c.CreatedByUserId == currentUser.Id);
                viewModel.OpenContracts = await _context.Contracts
                    .CountAsync(c => c.CreatedByUserId == currentUser.Id && c.Status == ContractStatus.Open);
                viewModel.ClosedContracts = await _context.Contracts
                    .CountAsync(c => c.CreatedByUserId == currentUser.Id && c.Status == ContractStatus.Closed);
                viewModel.TodayContracts = await _context.Contracts
                    .CountAsync(c => c.CreatedByUserId == currentUser.Id && c.CreatedAt.Date == DateTime.Today);

                viewModel.RecentContracts = await _context.Contracts
                    .Where(c => c.CreatedByUserId == currentUser.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
