using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? user,
            string? action,
            string? entityType,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(user))
            {
                query = query.Where(a => a.UserName.Contains(user) || (a.UserId != null && a.UserId.Contains(user)));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action.Contains(action));
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType.Contains(entityType));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt.Date <= toDate.Value.Date);
            }

            ViewBag.UserFilter = user;
            ViewBag.ActionFilter = action;
            ViewBag.EntityTypeFilter = entityType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var logs = await query
                .Take(300)
                .ToListAsync();

            return View(logs);
        }
    }
}
