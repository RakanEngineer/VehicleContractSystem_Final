using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class ContractViewModel
    {
        public Contract Contract { get; set; } = new();

        [Display(Name = "استلم البائع مبلغ وقدره")]
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المستلم يجب أن يكون صفراً أو أكثر")]
        public decimal InitialPaidAmount { get; set; }

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
            Provinces.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.NameAr
            });

        public IEnumerable<SelectListItem> CityList =>
            Cities.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameAr
            });
    }
}
