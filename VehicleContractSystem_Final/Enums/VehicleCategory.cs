using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Enums
{
    public enum VehicleCategory
    {
        [Display(Name = "Private")]
        Private = 1,

        [Display(Name = "Taxi")]
        Taxi = 2,

        [Display(Name = "Cargo")]
        Cargo = 3,

        [Display(Name = "Construction")]
        Construction = 4,

        [Display(Name = "Agricultural")]
        Agricultural = 5,

        [Display(Name = "TemporaryTest")]
        TemporaryTest = 6,

        [Display(Name = "Motorcycle")]
        Motorcycle = 7,

        [Display(Name = "Government")]
        Government = 8,

        [Display(Name = "Other")]
        Other = 9
    }
}
