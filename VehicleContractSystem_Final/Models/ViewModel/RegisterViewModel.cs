using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "رقم الهاتف")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare("Password", ErrorMessage = "كلمة المرور غير متطابقة")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [StringLength(13)]
        [Display(Name = "رقم الهوية")]
        public string PersonalNumber { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        [Display(Name = "البريد الإلكتروني أو رقم الهاتف")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; }
    }

    public class ExternalLoginViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [StringLength(13)]
        [Display(Name = "رقم الهوية")]
        public string PersonalNumber { get; set; } = string.Empty;

        [Display(Name = "رقم الهاتف")]
        public string? PhoneNumber { get; set; }
    }

    public class PendingUsersViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
        public bool CanSelectOffice { get; set; }
        public bool CanSelectRole { get; set; }
    }
}
