using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Services
{    
    public interface ICurrentUserService
    {
        Task<ApplicationUser?> GetCurrentUserAsync();
        Task<int?> GetCurrentUserOfficeIdAsync();
        Task<bool> IsSuperAdminAsync();
        Task<bool> IsOfficeAdminAsync();
        Task<bool> CanAccessContract(Contract contract);
        Task<bool> CanAccessOffice(int officeId);
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<int?> GetCurrentUserOfficeIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.OfficeId;
        }

        public async Task<bool> IsSuperAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            return await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }

        public async Task<bool> IsOfficeAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            return await _userManager.IsInRoleAsync(user, "OfficeAdmin");
        }

        public async Task<bool> CanAccessContract(Contract contract)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            // SuperAdmin can access everything
            if (await IsSuperAdminAsync())
                return true;

            // OfficeAdmin can access contracts in their office
            if (await IsOfficeAdminAsync() && contract.OfficeId == user.OfficeId)
                return true;

            // User can only access their own contracts
            if (contract.CreatedByUserId == user.Id)
                return true;

            return false;
        }

        public async Task<bool> CanAccessOffice(int officeId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            // SuperAdmin can access all offices
            if (await IsSuperAdminAsync())
                return true;

            // OfficeAdmin and User can only access their own office
            return user.OfficeId == officeId;
        }
    }
}
