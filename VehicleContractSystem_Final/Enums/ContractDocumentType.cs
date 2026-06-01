using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Enums
{
    public enum ContractDocumentType
    {
        [Display(Name = "هوية البائع")]
        SellerIdentity = 1,

        [Display(Name = "هوية المشتري")]
        BuyerIdentity = 2,

        [Display(Name = "سنوية المركبة")]
        VehicleRegistration = 3,

        [Display(Name = "إجازة السوق")]
        DrivingLicense = 4,

        [Display(Name = "وكالة قانونية")]
        LegalPowerOfAttorney = 5,

        [Display(Name = "صورة المركبة")]
        VehicleImage = 6,

        [Display(Name = "مستند آخر")]
        Other = 7
    }
}
