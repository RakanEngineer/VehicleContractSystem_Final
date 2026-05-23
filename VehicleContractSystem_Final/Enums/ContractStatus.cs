using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Enums
{
    public enum ContractStatus
    {
        [Display(Name = "مفتوح")]
        Open = 1,
        
        [Display(Name = "مغلق")]
        Closed = 2
    }
}
