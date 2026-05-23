using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Display(Name = "نوع المركبة")]
        public int VehicleTypeId { get; set; }

        [Required(ErrorMessage = "يرجى اختيار نوع المركبة")]
        [Display(Name = "نوع المركبة")]
        public VehicleType VehicleType { get; set; } = null!;

        [Required(ErrorMessage = "يرجى اختيار موديل المركبة")]
        [Display(Name = "موديل المركبة")]
        public int VehicleModelId { get; set; }

        [Display(Name = "موديل المركبة")]
        public VehicleModel VehicleModel { get; set; } = null!;

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
        [Display(Name = "رقم الشاصي")]
        public string ChassisNumber { get; set; } = string.Empty;

        [Display(Name = "اسم الموديل")]
        public string VehicleModelName { get; set; } = string.Empty;

        [Display(Name = "الماركة")]
        public string VehicleBrand { get; set; } = string.Empty;

        [Display(Name = "اللون")]
        public string VehicleColorName { get; set; } = string.Empty;

        [Display(Name = "فئة المركبة")]
        public string VehicleCategory { get; set; } = string.Empty;

        [Display(Name = "عدد العجلات")]
        public int NumberOfWheels { get; set; }
                      
       
        //[Required]
        //public string LicenseNumber { get; set; } = string.Empty;
        //public int ManufactureYear { get; set; } // جديد
        //public string ChassisNumber { get; set; } = string.Empty; // جديد

        //[Required]
        //public string VehicleModelName { get; set; } = string.Empty;

        //public string VehicleBrand { get; set; } = string.Empty;
        //public string VehicleColorName { get; set; } = string.Empty; // نسخة نصية من اللون
        //public string VehicleCategory { get; set; } = string.Empty;
        //[Required]
        //public string Color { get; set; } = string.Empty;
        //public int NumberOfWheels { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;
    
    }
}
