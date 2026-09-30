using System.ComponentModel.DataAnnotations;
using VehicleContractSystem_Final.Enums;

namespace VehicleContractSystem_Final.Models
{
    public class ContractDocument
    {
        public int Id { get; set; }

        [Display(Name = "العقد")]
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        [Display(Name = "المركبة")]
        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [MaxLength(450)]
        [Display(Name = "رفع بواسطة")]
        public string? UploadedByUserId { get; set; }
        public ApplicationUser? UploadedByUser { get; set; }

        [Display(Name = "نوع المستند")]
        public ContractDocumentType DocumentType { get; set; } = ContractDocumentType.Other;

        [MaxLength(255)]
        [Display(Name = "اسم الملف الأصلي")]
        public string OriginalFileName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Display(Name = "اسم الملف المخزن")]
        public string StoredFileName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "مسار الملف")]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "نوع المحتوى")]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(64)]
        [Display(Name = "بصمة SHA-256")]
        public string? Sha256Hash { get; set; }

        [MaxLength(100)]
        [Display(Name = "مزود التخزين")]
        public string? StorageProvider { get; set; }

        [MaxLength(50)]
        [Display(Name = "حالة فحص الحماية")]
        public string? MalwareScanStatus { get; set; }

        [Display(Name = "حجم الملف")]
        public long FileSize { get; set; }

        [MaxLength(1000)]
        [Display(Name = "الملاحظات")]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [Display(Name = "محذوف")]
        public bool IsDeleted { get; set; }
    }
}
