using System.Globalization;

namespace VehicleContractSystem_Final.Models
{
    public class Color
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";

        // Helper property for localization
        public string Name =>
            CultureInfo.CurrentCulture.Name == "ar" ? NameAr : NameEn;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
