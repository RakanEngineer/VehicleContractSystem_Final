using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class VehicleType
    {
        public int Id { get; set; }

        [Required]
        public string NameAr { get; set; } = string.Empty;

        [Required]
        public string NameEn { get; set; } = string.Empty;

        public ICollection<VehicleModel> VehicleModels { get; set; } = new List<VehicleModel>();
    }
}
