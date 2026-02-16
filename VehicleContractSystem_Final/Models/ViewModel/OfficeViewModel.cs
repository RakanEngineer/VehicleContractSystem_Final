namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class OfficeViewModel
    {
        public Office Office { get; set; } = new();
        public List<ApplicationUser> Staff { get; set; } = new();
        public int TotalContracts { get; set; }
        public int OpenContracts { get; set; }
        public int ClosedContracts { get; set; }
    }
}
