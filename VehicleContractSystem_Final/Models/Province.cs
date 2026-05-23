using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class Province
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المحافظة")]
        public string NameAr { get; set; } = string.Empty;

        [Required]
        [Display(Name = "اسم المحافظة بالإنجليزية")]
        public string NameEn { get; set; } = string.Empty;

        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}
