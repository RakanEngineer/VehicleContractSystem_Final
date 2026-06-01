using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Enums
{
    public enum VehicleStatus
    {
        [Display(Name = "متاحة")]
        Available = 0,

        [Display(Name = "عليها عقد")]
        UnderContract = 1,

        [Display(Name = "مباعة")]
        Sold = 2,

        [Display(Name = "محجوزة")]
        Reserved = 3,

        [Display(Name = "موقوفة")]
        Suspended = 4
    }
}
