using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VehicleContractSystem_Final.Resources;

namespace VehicleContractSystem_Final.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Contract", ResourceType = typeof(SharedResources))]
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        [Required]
        [Range(0, double.MaxValue, ErrorMessageResourceName = "InvalidAmount",
               ErrorMessageResourceType = typeof(SharedResources))]
        [Display(Name = "Amount", ResourceType = typeof(SharedResources))]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "PaymentDate", ResourceType = typeof(SharedResources))]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Display(Name = "PaymentMethod", ResourceType = typeof(SharedResources))]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer, Check

        [Display(Name = "Notes", ResourceType = typeof(SharedResources))]
        public string Notes { get; set; } = "";

        // Audit Fields
        [Display(Name = "RecordedBy", ResourceType = typeof(SharedResources))]
        public string RecordedByUserId { get; set; } = "";
        public ApplicationUser RecordedBy { get; set; } = null!;

        [Display(Name = "CreatedAt", ResourceType = typeof(SharedResources))]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
}
