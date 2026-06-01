using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class UserManagementViewModel
    {
        public List<UserManagementItemViewModel> Users { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
        public string? Search { get; set; }
        public int? OfficeId { get; set; }
        public string? Role { get; set; }
        public bool CanManageAllUsers { get; set; }
        public bool CanChangeRoles { get; set; }
        public bool CanAssignOffices { get; set; }
    }

    public class UserManagementItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PersonalNumber { get; set; } = string.Empty;
        public int? OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string RolesText { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
    }
}
