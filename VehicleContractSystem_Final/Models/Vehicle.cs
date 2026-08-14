using System.ComponentModel.DataAnnotations;
using VehicleContractSystem_Final.Enums;

namespace VehicleContractSystem_Final.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Display(Name = "صنف المركبة")]
        public int VehicleTypeId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار صنف المركبة")]
        [Display(Name = "صنف المركبة")]
        public VehicleType VehicleType { get; set; } = null!;

        [Display(Name = "نوع المركبة")]
        public int? VehicleModelId { get; set; }

        [Display(Name = "نوع المركبة")]
        public VehicleModel? VehicleModel { get; set; }

        [Required(ErrorMessage = "يرجى اختيار لون المركبة")]
        [Display(Name = "لون المركبة")]
        public int ColorId { get; set; }

        [Display(Name = "لون المركبة")]
        public Color Color { get; set; } = null!;

        [Required(ErrorMessage = "رقم المركبة مطلوب")]
        [Display(Name = "رقم المركبة")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "سنة الصنع")]
        public int ManufactureYear { get; set; }

        [Required(ErrorMessage = "رقم الشاصي مطلوب")]
        [MaxLength(450)]
        [Display(Name = "رقم الشاصي")]
        public string ChassisNumber { get; set; } = string.Empty;

        [Display(Name = "نوع المركبة")]
        public string VehicleModelName { get; set; } = string.Empty;

        [Display(Name = "الماركة")]
        public string VehicleBrand { get; set; } = string.Empty;

        [Display(Name = "اللون")]
        public string VehicleColorName { get; set; } = string.Empty;

        [Display(Name = "فئة المركبة")]
        public string VehicleCategory { get; set; } = string.Empty;

        [Display(Name = "عدد العجلات")]
        public int NumberOfWheels { get; set; }

        [Display(Name = "حالة المركبة")]
        public VehicleStatus Status { get; set; } = VehicleStatus.Available;

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<ContractDocument> Documents { get; set; } = new List<ContractDocument>();
    }
}
