using VehicleContractSystem_Final.Enums;

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
        public int ActiveOffices { get; set; }
        public int PendingUsers { get; set; }

        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int VehiclesUnderContract { get; set; }
        public int SoldVehicles { get; set; }

        public decimal TotalSalesAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }

        public List<DashboardContractSummary> RecentContracts { get; set; } = new();
        public List<TopPerformer> TopPerformers { get; set; } = new();
    }

    public class DashboardContractSummary
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ContractStatus Status { get; set; }
        public decimal VehiclePrice { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
