using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    /// <summary>
    /// يمثل المكتب القانوني أو معرض بيع السيارات
    /// كل مكتب يحتوي على مستخدمين وعقود خاصة به
    /// </summary>
    public class Office
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المكتب")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "رقم الإجازة")]
        [StringLength(100)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "العنوان")]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "رقم الهاتف")]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "تاريخ التأسيس")]
        public DateTime EstablishedDate { get; set; } = DateTime.Now;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // العلاقات - Relationships

        /// <summary>
        /// الموظفون/المستخدمون التابعون لهذا المكتب
        /// One-to-Many: مكتب واحد لديه عدة مستخدمين
        /// </summary>
        public ICollection<ApplicationUser> Staff { get; set; } = new List<ApplicationUser>();

        /// <summary>
        /// العقود التابعة لهذا المكتب
        /// One-to-Many: مكتب واحد لديه عدة عقود
        /// </summary>
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
