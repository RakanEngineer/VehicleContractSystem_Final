namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;

        public int TotalContracts { get; set; }
        public int OpenContracts { get; set; }
        public int ClosedContracts { get; set; }
        public int TodayContracts { get; set; }

        public int TotalUsers { get; set; }
        public int TotalOffices { get; set; }

        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int VehiclesUnderContract { get; set; }
        public int SoldVehicles { get; set; }

        public List<Contract> RecentContracts { get; set; } = new();
        public List<TopPerformer> TopPerformers { get; set; } = new();
    }
}
