using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "اسم المستخدم")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم الهوية")]
        public string PersonalNumber { get; set; } = string.Empty;

        [Display(Name = "المكتب")]
        public int? OfficeId { get; set; }

        [Required]
        [Display(Name = "الدور")]
        public string Role { get; set; } = "User";

        public List<Office> AvailableOffices { get; set; } = new();
    }
}
