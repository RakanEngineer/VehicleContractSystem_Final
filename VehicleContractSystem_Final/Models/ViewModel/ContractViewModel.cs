using Microsoft.AspNetCore.Mvc.Rendering;

namespace VehicleContractSystem_Final.Models.ViewModel
{
    public class ContractViewModel
    {
        public Contract Contract { get; set; } = new();
        public List<Province> Provinces { get; set; } = new();
        public List<City> Cities { get; set; } = new();

        public VehicleViewModel Vehicle { get; set; } = new VehicleViewModel();

        public IEnumerable<SelectListItem> VehicleTypes { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> VehicleModels { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Colors { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> ProvinceList =>
            Provinces.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.NameAr // أو استخدم Name حسب الثقافة
            });
        public IEnumerable<SelectListItem> CityList =>
            Cities.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameAr
            });
    }
}
