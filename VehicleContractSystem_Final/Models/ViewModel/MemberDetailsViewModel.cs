namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class MemberDetailsViewModel
    {
        public ApplicationUser User { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }
}
