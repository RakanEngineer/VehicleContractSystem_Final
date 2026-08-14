using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class ContractViewModel
    {
        private static readonly string[] VehicleProvinceOrder =
        [
            "بغداد",
            "نينوى",
            "الأنبار",
            "البصرة",
            "صلاح الدين",
            "النجف الأشرف",
            "كربلاء المقدسة",
            "كركوك",
            "ديالى",
            "واسط",
            "بابل",
            "القادسية",
            "المثنى",
            "ذي قار",
            "ميسان",
            "أربيل",
            "السليمانية",
            "دهوك",
            "حلبجة"
        ];

        public Contract Contract { get; set; } = new();

        [Display(Name = "استلم البائع مبلغ وقدره")]
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المستلم يجب أن يكون صفراً أو أكثر")]
        public decimal InitialPaidAmount { get; set; }

        [Display(Name = "لون المركبة")]
        public string? CustomColorName { get; set; }

        public List<Province> Provinces { get; set; } = new();
        public List<City> Cities { get; set; } = new();

        public IEnumerable<SelectListItem> VehicleTypes { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> VehicleModels { get; set; } = new List<SelectListItem>();
        public List<Color> Colors { get; set; } = new();

        public IEnumerable<SelectListItem> ColorList =>
            Colors.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameAr,
                Selected = Contract.ColorId == c.Id
            });

        public IEnumerable<SelectListItem> ProvinceList =>
            Provinces
                .OrderBy(GetVehicleProvinceOrder)
                .ThenBy(p => p.Id)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = GetVehicleProvinceDisplayName(p),
                    Selected = Contract.ProvinceId == p.Id
                });

        public IEnumerable<SelectListItem> CityList =>
            Cities.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameAr
            });

        private static string GetVehicleProvinceDisplayName(Province province)
        {
            return province.NameAr switch
            {
                "كربلاء" => "كربلاء المقدسة",
                "النجف" => "النجف الأشرف",
                _ => province.NameAr
            };
        }

        private static int GetVehicleProvinceOrder(Province province)
        {
            var index = Array.IndexOf(VehicleProvinceOrder, GetVehicleProvinceDisplayName(province));
            return index >= 0 ? index : int.MaxValue;
        }
    }
}
