namespace VehicleContractSystem_Final.Models
{
    public class Color
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
