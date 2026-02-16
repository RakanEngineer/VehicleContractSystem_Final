using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace VehicleContractSystem_Final.Models
{
    public class Province
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المحافظة")]
        public string NameAr { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Province Name")]
        public string NameEn { get; set; } = string.Empty;
        public string Name =>
        CultureInfo.CurrentCulture.Name == "ar" ? NameAr : NameEn;

        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}
