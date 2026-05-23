using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models
{
    public class VehicleModel
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public int VehicleTypeId { get; set; }
        [Display(Name = "نوع المركبة")]
        public VehicleType VehicleType { get; set; } = null!;

    }
}
