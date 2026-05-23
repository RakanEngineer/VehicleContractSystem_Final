using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleContractSystem_Final.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "العقد")]
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون مبلغ الدفعة أكبر من صفر")]
        [Display(Name = "المبلغ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "تاريخ الدفعة")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "نقداً";

        [Display(Name = "الملاحظات")]
        public string Notes { get; set; } = "";

        [Display(Name = "مسجل الدفعة")]
        public string RecordedByUserId { get; set; } = "";
        public ApplicationUser RecordedBy { get; set; } = null!;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
