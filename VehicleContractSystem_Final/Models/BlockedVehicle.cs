using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class BlockedVehicle
    {
        public int Id { get; set; }

        [MaxLength(450)]
        [Display(Name = "رقم الشاصي")]
        public string? ChassisNumber { get; set; }

        [MaxLength(450)]
        [Display(Name = "رقم المركبة")]
        public string? LicenseNumber { get; set; }

        [MaxLength(50)]
        [Display(Name = "رمز اللوحة")]
        public string? PlateCode { get; set; }

        [Required(ErrorMessage = "يرجى إدخال سبب المنع")]
        [MaxLength(1000)]
        [Display(Name = "سبب المنع")]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "المصدر")]
        public string Source { get; set; } = "إدخال يدوي";

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(450)]
        [Display(Name = "منشئ السجل")]
        public string? CreatedByUserId { get; set; }

        public ApplicationUser? CreatedByUser { get; set; }

        [Display(Name = "تاريخ التعطيل")]
        public DateTime? DeactivatedAt { get; set; }

        [MaxLength(450)]
        [Display(Name = "تم التعطيل بواسطة")]
        public string? DeactivatedByUserId { get; set; }

        public ApplicationUser? DeactivatedByUser { get; set; }

        [MaxLength(1000)]
        [Display(Name = "سبب التعطيل")]
        public string? DeactivationReason { get; set; }
    }
}
