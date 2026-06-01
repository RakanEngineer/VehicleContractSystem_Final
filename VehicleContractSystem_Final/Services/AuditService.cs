using System.Security.Claims;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityType, string entityId, string description)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = user?.Identity?.Name ?? "النظام";
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
