using Microsoft.AspNetCore.Authorization;
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
        private readonly ICurrentUserService _currentUserService;

        public HomeController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View("Public");

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Challenge();

            var viewModel = new DashboardViewModel
            {
                UserName = currentUser.FullName
            };

            IQueryable<Contract> contractsQuery;
            IQueryable<Vehicle> vehiclesQuery;

            if (await _currentUserService.IsSuperAdminAsync())
            {
                viewModel.UserRole = "مدير النظام";
                contractsQuery = _context.Contracts.AsNoTracking();
                vehiclesQuery = _context.Vehicles.AsNoTracking();

                viewModel.TotalUsers = await _context.Users.CountAsync();
                viewModel.PendingUsers = await _context.Users.CountAsync(u => !u.IsApproved);
                viewModel.TotalOffices = await _context.Offices.CountAsync();
                viewModel.ActiveOffices = await _context.Offices.CountAsync(o => o.IsActive);
            }
            else if (await _currentUserService.IsOfficeAdminAsync())
            {
                viewModel.UserRole = "مدير المكتب";
                var office = await _context.Offices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == currentUser.OfficeId);
                viewModel.OfficeName = office?.Name ?? string.Empty;

                contractsQuery = _context.Contracts
                    .AsNoTracking()
                    .Where(c => c.OfficeId == currentUser.OfficeId);

                var officeVehicleIds = contractsQuery
                    .Where(c => c.VehicleId != null)
                    .Select(c => c.VehicleId!.Value)
                    .Distinct();

                vehiclesQuery = _context.Vehicles
                    .AsNoTracking()
                    .Where(v => officeVehicleIds.Contains(v.Id));

                viewModel.TotalUsers = await _context.Users.CountAsync(u => u.OfficeId == currentUser.OfficeId);
            }
            else
            {
                viewModel.UserRole = "موظف";
                var office = await _context.Offices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == currentUser.OfficeId);
                viewModel.OfficeName = office?.Name ?? string.Empty;

                contractsQuery = _context.Contracts
                    .AsNoTracking()
                    .Where(c => c.CreatedByUserId == currentUser.Id);

                var userVehicleIds = contractsQuery
                    .Where(c => c.VehicleId != null)
                    .Select(c => c.VehicleId!.Value)
                    .Distinct();

                vehiclesQuery = _context.Vehicles
                    .AsNoTracking()
                    .Where(v => userVehicleIds.Contains(v.Id));
            }

            await PopulateContractStatsAsync(viewModel, contractsQuery);
            await PopulateVehicleStatsAsync(viewModel, vehiclesQuery);
            viewModel.RecentContracts = await BuildRecentContractsAsync(contractsQuery);

            if (await _currentUserService.IsSuperAdminAsync() || await _currentUserService.IsOfficeAdminAsync())
            {
                viewModel.TopPerformers = await BuildTopPerformersAsync(contractsQuery);
            }

            return View(viewModel);
        }

        private static async Task PopulateContractStatsAsync(
            DashboardViewModel viewModel,
            IQueryable<Contract> contracts)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            viewModel.TotalContracts = await contracts.CountAsync();
            viewModel.OpenContracts = await contracts.CountAsync(c => c.Status == ContractStatus.Open);
            viewModel.ClosedContracts = await contracts.CountAsync(c => c.Status == ContractStatus.Closed);
            viewModel.TodayContracts = await contracts.CountAsync(c => c.CreatedAt >= today && c.CreatedAt < tomorrow);
            viewModel.TotalSalesAmount = await contracts.SumAsync(c => (decimal?)c.VehiclePrice) ?? 0;
            viewModel.TotalPaidAmount = await contracts
                .SelectMany(c => c.Payments)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            viewModel.TotalRemainingAmount = viewModel.TotalSalesAmount - viewModel.TotalPaidAmount;
        }

        private static async Task PopulateVehicleStatsAsync(DashboardViewModel viewModel, IQueryable<Vehicle> vehicles)
        {
            viewModel.TotalVehicles = await vehicles.CountAsync();
            viewModel.AvailableVehicles = await vehicles.CountAsync(v => v.Status == VehicleStatus.Available);
            viewModel.VehiclesUnderContract = await vehicles.CountAsync(v => v.Status == VehicleStatus.UnderContract);
            viewModel.SoldVehicles = await vehicles.CountAsync(v => v.Status == VehicleStatus.Sold);
        }

        private static async Task<List<DashboardContractSummary>> BuildRecentContractsAsync(IQueryable<Contract> contracts)
        {
            return await contracts
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new DashboardContractSummary
                {
                    Id = c.Id,
                    ContractNumber = c.ContractNumber,
                    SellerName = c.SellerName,
                    BuyerName = c.BuyerName,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status,
                    VehiclePrice = c.VehiclePrice,
                    PaidAmount = c.Payments.Select(p => (decimal?)p.Amount).Sum() ?? 0,
                    RemainingAmount = c.VehiclePrice - (c.Payments.Select(p => (decimal?)p.Amount).Sum() ?? 0)
                })
                .Take(5)
                .ToListAsync();
        }

        private static async Task<List<TopPerformer>> BuildTopPerformersAsync(IQueryable<Contract> contracts)
        {
            return await contracts
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

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessRequired(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index));

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
