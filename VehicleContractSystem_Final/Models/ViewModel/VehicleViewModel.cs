using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class VehicleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "يرجى اختيار نوع المركبة")]
        [Display(Name = "نوع المركبة")]
        public int? VehicleTypeId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار موديل المركبة")]
        [Display(Name = "موديل المركبة")]
        public int? VehicleModelId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار لون المركبة")]
        [Display(Name = "لون المركبة")]
        public int? ColorId { get; set; }

        public int ManufactureYear { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string ChassisNumber { get; set; } = string.Empty;
    }
}
