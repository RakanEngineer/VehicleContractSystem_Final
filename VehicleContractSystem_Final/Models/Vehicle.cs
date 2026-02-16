using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public int? VehicleTypeId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle type.")]
        public VehicleType? VehicleType { get; set; }

        [Required]
        public int VehicleModelId { get; set; }  // جديد
        public VehicleModel? VehicleModel { get; set; } // جديد

        [Required]

        public int ColorId { get; set; } // جديد
        public Color? Color { get; set; } // جديد


        [Required]
        public string LicenseNumber { get; set; } = string.Empty;
        public int ManufactureYear { get; set; } // جديد
        public string ChassisNumber { get; set; } = string.Empty; // جديد

        [Required]
        public string VehicleModelName { get; set; } = string.Empty;

        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleColorName { get; set; } = string.Empty; // نسخة نصية من اللون
        public string VehicleCategory { get; set; } = string.Empty;
        //[Required]
        //public string Color { get; set; } = string.Empty;

        public int NumberOfWheels { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser? Owner { get; set; }
    
    }
}
