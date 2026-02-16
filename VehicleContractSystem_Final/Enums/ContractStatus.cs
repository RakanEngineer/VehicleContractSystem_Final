using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Enums
{
    public enum ContractStatus
    {
        [Display(Name = "Open")]
        Open = 1,
        
        [Display(Name = "Closed")]
        Closed = 2
    }
}
