using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المدينة")]
        public string NameAr { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City Name")]
        public string NameEn { get; set; } = string.Empty;

        [Required]
        public int ProvinceId { get; set; }
        public Province? Province { get; set; }
    }
}
