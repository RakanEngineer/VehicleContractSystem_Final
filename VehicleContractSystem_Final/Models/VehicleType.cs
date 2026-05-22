using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace VehicleContractSystem_Final.Models
{
    public class VehicleType
    {
        public int Id { get; set; }

        [Required]
        public string NameAr { get; set; } = string.Empty;

        [Required]
        public string NameEn { get; set; } = string.Empty;

        // Helper property for localization
        //public string Name =>
        //    CultureInfo.CurrentCulture.Name == "ar" ? NameAr : NameEn;
        public string Name
        {
            get
            {
                return CultureInfo.CurrentCulture.Name.StartsWith("ar") ? NameAr : NameEn;
            }
        }

        public ICollection<VehicleModel> VehicleModels { get; set; } = new List<VehicleModel>();
    }
}
