namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class ContractVerificationViewModel
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public DateTime? ContractDate { get; set; }
        public string ContractStatus { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public string ChassisNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
    }
}
