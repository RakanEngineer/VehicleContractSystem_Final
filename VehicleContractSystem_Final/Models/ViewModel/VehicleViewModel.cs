namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class VehicleViewModel
    {
        public int Id { get; set; }
        public int VehicleTypeId { get; set; }
        public int VehicleModelId { get; set; }
        public int ColorId { get; set; }
        public int ManufactureYear { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string ChassisNumber { get; set; } = string.Empty;
    }
}
