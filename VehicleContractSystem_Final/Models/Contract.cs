using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Models
{
    /// <summary>
    /// عقد بيع وشراء سيارة
    /// كل عقد ينتمي لمستخدم محدد ومكتب محدد
    /// </summary>
    public class Contract
    {
        public int Id { get; set; }

        [Display(Name = "رقم العقد")]
        public string ContractNumber { get; set; } = string.Empty;

        [Display(Name = "تاريخ العقد")]
        [DataType(DataType.Date)]
        public DateTime ContractDate { get; set; } = DateTime.Now;

        // بيانات البائع - Seller Information
        // ===================================================

        [Required]
        [Display(Name = "اسم البائع")]
        public string SellerName { get; set; } = string.Empty;

        [Display(Name = "عنوان البائع")]
        public string SellerAddress { get; set; } = string.Empty;

        [Display(Name = "مهنة البائع")]
        public string SellerOccupation { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم هاتف البائع")]
        public string SellerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم هوية البائع")]
        public string SellerIdNumber { get; set; } = string.Empty;

        [Display(Name = "جهة إصدار هوية البائع")]
        public string SellerIdIssuer { get; set; } = string.Empty;

        // بيانات المشتري - Buyer Information
        // ===================================================

        [Required]
        [Display(Name = "اسم المشتري")]
        public string BuyerName { get; set; } = string.Empty;

        [Display(Name = "عنوان المشتري")]
        public string BuyerAddress { get; set; } = string.Empty;

        [Display(Name = "مهنة المشتري")]
        public string BuyerOccupation { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم هاتف المشتري")]
        public string BuyerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم هوية المشتري")]
        public string BuyerIdNumber { get; set; } = string.Empty;

        [Display(Name = "جهة إصدار هوية المشتري")]
        public string BuyerIdIssuer { get; set; } = string.Empty;

        // بيانات السيارة - Vehicle Information
        // ===================================================

        [Required]
        [Display(Name = "اسم المالك")]
        public string CurrentOwnerName { get; set; } = string.Empty;

        [Display(Name = "عنوان المالك")]
        public string CurrentOwnerAddress { get; set; } = string.Empty;

        [Display(Name = "رقم السنوية")]
        public string AnnualRegistrationNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الماركة")]
        public string VehicleBrand { get; set; } = string.Empty;

        [Required]
        [Display(Name = "الموديل")]
        public string VehicleModel { get; set; } = string.Empty;

        [Required]
        [Display(Name = "سنة الصنع")]
        public int ManufactureYear { get; set; }

        [Display(Name = "النوع")]
        public string VehicleCategory { get; set; } = string.Empty;

        [Display(Name = "اللون")]
        public string VehicleColor { get; set; } = string.Empty;

        [Required]
        [Display(Name = "رقم الشاصي")]
        public string ChassisNumber { get; set; } = string.Empty;
        [Required]
        [Display(Name = "رقم السيارة")]
        public string LicenseNumber { get; set; } = string.Empty;

        // معلومات العنوان - Address Information
        // ===================================================

        [Display(Name = "المحافظة")]
        public int? ProvinceId { get; set; }
        public Province? Province { get; set; }

        [Display(Name = "المدينة")]
        public int? CityId { get; set; }
        public City? City { get; set; }

        [Display(Name = "المحلة")]
        public string Neighborhood { get; set; } = string.Empty;

        [Display(Name = "رقم الزقاق")]
        public string AlleyNumber { get; set; } = string.Empty;

        [Display(Name = "رقم الدار")]
        public string HouseNumber { get; set; } = string.Empty;

        // معلومات إضافية - Additional Information
        // ===================================================

        [Display(Name = "الملاحظات")]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "حالة العقد")]
        public ContractStatus Status { get; set; } //= "Open"; // Open, Closed

        // العلاقات - Relationships (الجزء الأهم!)
        // ===================================================

        /// <summary>
        /// رقم المستخدم الذي أنشأ العقد
        /// هذا يحدد من هو صاحب العقد ومن يملك صلاحية تعديله
        /// </summary>
        [Required]
        [Display(Name = "منشئ العقد")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// المستخدم الذي أنشأ العقد
        /// Many-to-One: عدة عقود يمكن أن تُنشأ بواسطة مستخدم واحد
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }

        /// <summary>
        /// رقم المكتب التابع له العقد
        /// هذا يحدد عزل البيانات - كل مكتب يرى عقوده فقط
        /// </summary>
        [Required]
        [Display(Name = "المكتب")]
        public int OfficeId { get; set; }

        /// <summary>
        /// المكتب التابع له العقد
        /// Many-to-One: عدة عقود تنتمي لمكتب واحد
        /// </summary>
        public Office? Office { get; set; }

        // معلومات التتبع - Audit Information
        // ===================================================

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ آخر تعديل")]
        public DateTime? LastModifiedAt { get; set; }

        [Display(Name = "آخر من عدّل")]
        public string? LastModifiedByUserId { get; set; }

        ////
        // المبلغ الإجمالي للسيارة
        [Display(Name = "سعر المركبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VehiclePrice { get; set; } = 0;

        // المدفوعات
        public List<Payment> Payments { get; set; } = new();

        [Display(Name = "المبلغ المدفوع")]
        public decimal PaidAmount => Payments.Sum(p => p.Amount);

        [Display(Name = "المبلغ المتبقي")]
        public decimal RemainingAmount => VehiclePrice - PaidAmount;

        // اختياري: كائن مركبة لتسهيل الاستخدام
        [NotMapped]
        public Vehicle? Vehicle { get; set; } // فقط لواجهة المستخدم أو ViewModel
    }
}
