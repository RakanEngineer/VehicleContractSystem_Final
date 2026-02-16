using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    /// <summary>
    /// المستخدم في النظام - موظف في أحد المكاتب
    /// كل مستخدم ينتمي لمكتب واحد فقط
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(13)]
        [Display(Name = "رقم الهوية الشخصي")]
        public string PersonalNumber { get; set; } = string.Empty;

        [Display(Name = "عضو مميز")]
        public bool IsProMember { get; set; } = false;

        // حقول الموافقة والتفعيل - Approval Fields
        // ===================================================

        /// <summary>
        /// هل تم تفعيل الحساب من قبل Admin؟
        /// false = ينتظر الموافقة، true = مفعّل
        /// </summary>
        [Display(Name = "مفعّل")]
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// تاريخ الموافقة
        /// </summary>
        [Display(Name = "تاريخ الموافقة")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// من وافق على الحساب
        /// </summary>
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// تاريخ التسجيل
        /// </summary>
        [Display(Name = "تاريخ التسجيل")]
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // ربط المستخدم بالمكتب - Office Relationship
        // ===================================================

        /// <summary>
        /// رقم المكتب التابع له المستخدم
        /// null = Super Admin (لا ينتمي لمكتب محدد)
        /// </summary>
        [Display(Name = "المكتب التابع له")]
        public int? OfficeId { get; set; }

        /// <summary>
        /// المكتب التابع له المستخدم
        /// Many-to-One: عدة مستخدمين ينتمون لمكتب واحد
        /// </summary>
        public Office? Office { get; set; }

        // العلاقات الأخرى - Other Relationships
        // ===================================================

        /// <summary>
        /// المركبات التابعة لهذا المستخدم (للاستخدام المستقبلي)
        /// </summary>
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        /// <summary>
        /// العقود التي أنشأها هذا المستخدم
        /// One-to-Many: مستخدم واحد يمكنه إنشاء عدة عقود
        /// </summary>
        public ICollection<Contract> CreatedContracts { get; set; } = new List<Contract>();

        /// <summary>
        /// الإشعارات الخاصة بهذا المستخدم
        /// One-to-Many: مستخدم واحد لديه عدة إشعارات
        /// </summary>
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // خصائص محسوبة - Computed Properties
        // ===================================================

        /// <summary>
        /// الاسم الكامل
        /// </summary>
        [Display(Name = "الاسم الكامل")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
