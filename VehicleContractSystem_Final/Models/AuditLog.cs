using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(450)]
        [Display(Name = "معرف المستخدم")]
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [MaxLength(256)]
        [Display(Name = "اسم المستخدم")]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "الإجراء")]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "نوع الكيان")]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "معرف الكيان")]
        public string EntityId { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(64)]
        [Display(Name = "عنوان IP")]
        public string IpAddress { get; set; } = string.Empty;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
