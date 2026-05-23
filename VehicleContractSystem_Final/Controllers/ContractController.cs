using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Helpers;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Models.ViewModel;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize]
    public class ContractController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;

        public ContractController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _userManager = userManager;
            _currentUserService = currentUserService;
        }

        // GET: Contract
        public async Task<IActionResult> Index()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Challenge();

            IQueryable<Contract> query = _context.Contracts
                .Include(c => c.Office)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Province)
                .Include(c => c.City);

            // Super Admin: sees all contracts
            if (await _currentUserService.IsSuperAdminAsync())
            {
                var allContracts = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                ViewBag.ViewMode = "all";
                return View(allContracts);
            }

            // Office Admin: sees all contracts in their office
            if (await _currentUserService.IsOfficeAdminAsync())
            {
                var officeContracts = await query
                    .Where(c => c.OfficeId == currentUser.OfficeId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                ViewBag.ViewMode = "office";
                return View(officeContracts);
            }

            // User: sees only their own contracts
            var userContracts = await query
                .Where(c => c.CreatedByUserId == currentUser.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            ViewBag.ViewMode = "personal";
            return View(userContracts);
        }

        // GET: Contract/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Office)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Province)
                .Include(c => c.City)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية الوصول لهذا العقد";
                return RedirectToAction(nameof(Index));
            }

            return View(contract);
        }

        // GET: Contract/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.OfficeId == null)
            {
                TempData["ErrorMessage"] = "يجب أن تكون منتمياً لمكتب لإنشاء عقد";
                return RedirectToAction(nameof(Index));
            }

            var contract = new Contract
            {
                ContractNumber = GenerateContractNumber(),
                ContractDate = DateTime.Now,
                OfficeId = currentUser.OfficeId.Value
            };

            var viewModel = await BuildContractViewModelAsync(contract);

            return View(viewModel);
        }

        // POST: Contract/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractViewModel viewModel)
        {
            var contract = viewModel.Contract;
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.OfficeId == null)
            {
                TempData["ErrorMessage"] = "يجب أن تكون منتمياً لمكتب لإنشاء عقد";
                return RedirectToAction(nameof(Index));
            }

            // Remove validation for navigation properties
            ModelState.Remove("Contract.ContractNumber");
            ModelState.Remove("Contract.CreatedByUser");
            ModelState.Remove("Contract.CreatedByUserId");
            ModelState.Remove("Contract.Office");
            ModelState.Remove("Contract.OfficeId");
            ModelState.Remove("Contract.Province");
            ModelState.Remove("Contract.City");
            ModelState.Remove("Contract.Payments");
            ModelState.Remove("Contract.VehicleCategory");
            ModelState.Remove("Contract.VehicleModel");
            ModelState.Remove("Contract.VehicleColor");
            await ApplyVehicleLookupValuesAsync(contract);
            await ValidateVehicleLookupsAsync(contract);

            if (ModelState.IsValid)
            {
                contract.CreatedByUserId = currentUser.Id;
                contract.OfficeId = currentUser.OfficeId.Value;
                contract.CreatedAt = DateTime.Now;
                contract.Status = ContractStatus.Open;

                if (string.IsNullOrWhiteSpace(contract.ContractNumber))
                {
                    contract.ContractNumber = GenerateContractNumber();
                }

                _context.Add(contract);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم إنشاء العقد بنجاح";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            LogModelStateErrors();
            viewModel = await BuildContractViewModelAsync(contract);

            return View(viewModel);
        }

        // GET: Contract/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية تعديل هذا العقد";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (contract.Status == ContractStatus.Closed)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل عقد مغلق";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            await EnsureContractLookupIdsAsync(contract);
            var viewModel = await BuildContractViewModelAsync(contract);

            return View(viewModel);
        }

        // POST: Contract/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContractViewModel viewModel)
        {
            var contract = viewModel.Contract;
            if (id != contract.Id)
                return NotFound();

            var existingContract = await _context.Contracts.FindAsync(id);
            if (existingContract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(existingContract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية تعديل هذا العقد";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (existingContract.Status == ContractStatus.Closed)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل عقد مغلق";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            contract.ContractNumber = existingContract.ContractNumber;

            // Remove validation for navigation properties
            ModelState.Remove("Contract.ContractNumber");
            ModelState.Remove("Contract.CreatedByUser");
            ModelState.Remove("Contract.CreatedByUserId");
            ModelState.Remove("Contract.Office");
            ModelState.Remove("Contract.OfficeId");
            ModelState.Remove("Contract.Province");
            ModelState.Remove("Contract.City");
            ModelState.Remove("Contract.Payments");
            ModelState.Remove("Contract.VehicleCategory");
            ModelState.Remove("Contract.VehicleModel");
            ModelState.Remove("Contract.VehicleColor");
            await ApplyVehicleLookupValuesAsync(contract);
            await ValidateVehicleLookupsAsync(contract);

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only allowed fields
                    existingContract.SellerName = contract.SellerName;
                    existingContract.SellerAddress = contract.SellerAddress;
                    existingContract.SellerOccupation = contract.SellerOccupation;
                    existingContract.SellerPhone = contract.SellerPhone;
                    existingContract.SellerIdNumber = contract.SellerIdNumber;
                    existingContract.SellerIdIssuer = contract.SellerIdIssuer;
                    existingContract.BuyerName = contract.BuyerName;
                    existingContract.BuyerAddress = contract.BuyerAddress;
                    existingContract.BuyerOccupation = contract.BuyerOccupation;
                    existingContract.BuyerPhone = contract.BuyerPhone;
                    existingContract.BuyerIdNumber = contract.BuyerIdNumber;
                    existingContract.BuyerIdIssuer = contract.BuyerIdIssuer;
                    existingContract.CurrentOwnerName = contract.CurrentOwnerName;
                    existingContract.CurrentOwnerAddress = contract.CurrentOwnerAddress;
                    existingContract.AnnualRegistrationNumber = contract.AnnualRegistrationNumber;
                    existingContract.VehicleBrand = contract.VehicleBrand;
                    existingContract.VehicleTypeId = contract.VehicleTypeId;
                    existingContract.VehicleModelId = contract.VehicleModelId;
                    existingContract.ColorId = contract.ColorId;
                    existingContract.VehicleModel = contract.VehicleModel;
                    existingContract.ManufactureYear = contract.ManufactureYear;
                    existingContract.VehicleCategory = contract.VehicleCategory;
                    existingContract.VehicleColor = contract.VehicleColor;
                    existingContract.ChassisNumber = contract.ChassisNumber;
                    existingContract.LicenseNumber = contract.LicenseNumber;
                    existingContract.VehiclePrice = contract.VehiclePrice;
                    existingContract.Notes = contract.Notes;
                    existingContract.ProvinceId = contract.ProvinceId;
                    existingContract.CityId = contract.CityId;
                    existingContract.Neighborhood = contract.Neighborhood;
                    existingContract.AlleyNumber = contract.AlleyNumber;
                    existingContract.HouseNumber = contract.HouseNumber;
                    existingContract.LastModifiedAt = DateTime.Now;
                    existingContract.LastModifiedByUserId = (await _currentUserService.GetCurrentUserAsync())?.Id;

                    _context.Update(existingContract);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم تحديث العقد بنجاح";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            LogModelStateErrors();
            viewModel = await BuildContractViewModelAsync(contract);

            return View(viewModel);
        }

        // POST: Contract/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية إغلاق هذا العقد";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            contract.Status = ContractStatus.Closed;
            contract.LastModifiedAt = DateTime.Now;
            contract.LastModifiedByUserId = (await _currentUserService.GetCurrentUserAsync())?.Id;

            _context.Update(contract);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إغلاق العقد بنجاح";
            return RedirectToAction(nameof(Details), new { id = contract.Id });
        }

        // GET: Contract/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Office)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Province)
                .Include(c => c.City)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية حذف هذا العقد";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (contract.Status == ContractStatus.Closed)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف عقد مغلق";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            return View(contract);
        }

        // POST: Contract/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية حذف هذا العقد";
                return RedirectToAction(nameof(Index));
            }

            if (contract.Status == ContractStatus.Closed)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف عقد مغلق";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (contract.Payments.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن حذف عقد يحتوي على دفعات";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف العقد بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetVehicleModelsByType(int vehicleTypeId)
        {
            var models = await _context.VehicleModels
                .Where(m => m.VehicleTypeId == vehicleTypeId)
                .Select(m => new { value = m.Id, text = m.NameAr })
                .ToListAsync();

            return Json(models);
        }

        // GET: Contract/GetCities
        [HttpGet]
        public async Task<IActionResult> GetCities(int provinceId)
        {
            var cities = await _context.Cities
                .Where(c => c.ProvinceId == provinceId)
                .Select(c => new { value = c.Id, text = c.NameAr })
                .ToListAsync();

            return Json(cities);
        }

        // GET: Contract/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Office)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Province)
                .Include(c => c.City)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية طباعة هذا العقد";
                return RedirectToAction(nameof(Index));
            }

            // Generate QR Code
            var qrCodeBase64 = QRCodeHelper.GenerateQRCodeForContract(
                contract.ContractNumber,
                contract.ContractDate,
                contract.SellerName,
                contract.BuyerName
            );

            ViewBag.QRCodeBase64 = qrCodeBase64;

            return View(contract);
        }

        // GET: Contract/ExportPDF/5
        public async Task<IActionResult> ExportPDF(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Office)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Province)
                .Include(c => c.City)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية تصدير هذا العقد";
                return RedirectToAction(nameof(Index));
            }

            var pdfBytes = GeneratePDF(contract);
            return File(pdfBytes, "application/pdf", $"Contract_{contract.ContractNumber}.pdf");
        }

        private byte[] GeneratePDF(Contract contract)
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 50, 50, 50, 50);
            var writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            var arabicFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12);
            var titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16, Font.BOLD);

            var title = new Paragraph("عقد بيع وشراء سيارة", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            document.Add(title);
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph($"رقم العقد: {contract.ContractNumber}", arabicFont));
            document.Add(new Paragraph($"تاريخ العقد: {contract.ContractDate:yyyy-MM-dd}", arabicFont));
            if (contract.Office != null)
                document.Add(new Paragraph($"المكتب: {contract.Office.Name}", arabicFont));
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph("بيانات البائع:", titleFont));
            document.Add(new Paragraph($"الاسم: {contract.SellerName}", arabicFont));
            document.Add(new Paragraph($"رقم الهوية: {contract.SellerIdNumber}", arabicFont));
            document.Add(new Paragraph($"رقم الهاتف: {contract.SellerPhone}", arabicFont));
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph("بيانات المشتري:", titleFont));
            document.Add(new Paragraph($"الاسم: {contract.BuyerName}", arabicFont));
            document.Add(new Paragraph($"رقم الهوية: {contract.BuyerIdNumber}", arabicFont));
            document.Add(new Paragraph($"رقم الهاتف: {contract.BuyerPhone}", arabicFont));
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph("بيانات السيارة:", titleFont));
            document.Add(new Paragraph($"الماركة: {contract.VehicleBrand}", arabicFont));
            document.Add(new Paragraph($"الموديل: {contract.VehicleModel}", arabicFont));
            document.Add(new Paragraph($"سنة الصنع: {contract.ManufactureYear}", arabicFont));
            document.Add(new Paragraph($"رقم الشاصي: {contract.ChassisNumber}", arabicFont));

            var qrCodeBase64 = QRCodeHelper.GenerateQRCodeForContract(
                contract.ContractNumber,
                contract.ContractDate,
                contract.SellerName,
                contract.BuyerName
            );

            var qrBytes = Convert.FromBase64String(qrCodeBase64);
            var qrImage = Image.GetInstance(qrBytes);
            qrImage.ScaleToFit(100f, 100f);
            qrImage.Alignment = Element.ALIGN_CENTER;
            document.Add(qrImage);

            document.Close();

            return ms.ToArray();
        }

        private string GenerateContractNumber()
        {
            var year = DateTime.Now.Year;
            var random = new Random();
            var number = random.Next(1000, 9999);
            return $"CT-{year}-{number}";
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.Id == id);
        }

        private async Task<ContractViewModel> BuildContractViewModelAsync(Contract contract)
        {
            return new ContractViewModel
            {
                Contract = contract,
                Provinces = await _context.Provinces.ToListAsync(),
                Cities = contract.ProvinceId.HasValue
                    ? await _context.Cities.Where(c => c.ProvinceId == contract.ProvinceId).ToListAsync()
                    : new List<City>(),
                VehicleTypes = await _context.VehicleTypes
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.NameAr,
                        Selected = contract.VehicleTypeId == v.Id
                    }).ToListAsync(),
                VehicleModels = await _context.VehicleModels
                    .Where(v => !contract.VehicleTypeId.HasValue || v.VehicleTypeId == contract.VehicleTypeId.Value)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.NameAr,
                        Selected = contract.VehicleModelId == v.Id
                    }).ToListAsync(),
                Colors = await _context.Colors
                    .OrderBy(c => c.Id)
                    .ToListAsync()
            };
        }

        private async Task EnsureContractLookupIdsAsync(Contract contract)
        {
            if (contract.VehicleModelId.HasValue && contract.ColorId.HasValue && contract.VehicleTypeId.HasValue)
                return;

            var vehicleModel = await _context.VehicleModels
                .FirstOrDefaultAsync(m => m.NameAr == contract.VehicleModel);
            var color = await _context.Colors
                .FirstOrDefaultAsync(c => c.NameAr == contract.VehicleColor);

            contract.VehicleModelId ??= vehicleModel?.Id;
            contract.VehicleTypeId ??= vehicleModel?.VehicleTypeId;
            contract.ColorId ??= color?.Id;
        }

        private async Task ApplyVehicleLookupValuesAsync(Contract contract)
        {
            var vehicleType = contract.VehicleTypeId.HasValue
                ? await _context.VehicleTypes.FindAsync(contract.VehicleTypeId.Value)
                : null;
            var vehicleModel = contract.VehicleModelId.HasValue
                ? await _context.VehicleModels.FindAsync(contract.VehicleModelId.Value)
                : null;
            var color = contract.ColorId.HasValue
                ? await _context.Colors.FindAsync(contract.ColorId.Value)
                : null;

            contract.VehicleCategory = vehicleType?.NameAr ?? string.Empty;
            contract.VehicleModel = vehicleModel?.NameAr ?? string.Empty;
            contract.VehicleColor = color?.NameAr ?? string.Empty;
        }

        private async Task ValidateVehicleLookupsAsync(Contract contract)
        {
            if (!contract.ColorId.HasValue || !await _context.Colors.AnyAsync(c => c.Id == contract.ColorId.Value))
                ModelState.AddModelError("Contract.ColorId", "يرجى اختيار لون مركبة صحيح");

            if (!contract.VehicleTypeId.HasValue || !await _context.VehicleTypes.AnyAsync(t => t.Id == contract.VehicleTypeId.Value))
                ModelState.AddModelError("Contract.VehicleTypeId", "يرجى اختيار نوع مركبة صحيح");

            if (!contract.VehicleModelId.HasValue || !await _context.VehicleModels.AnyAsync(m => m.Id == contract.VehicleModelId.Value))
            {
                ModelState.AddModelError("Contract.VehicleModelId", "يرجى اختيار موديل مركبة صحيح");
                return;
            }

            if (contract.VehicleTypeId.HasValue &&
                !await _context.VehicleModels.AnyAsync(m => m.Id == contract.VehicleModelId.Value && m.VehicleTypeId == contract.VehicleTypeId.Value))
            {
                ModelState.AddModelError("Contract.VehicleModelId", "موديل المركبة لا يتبع نوع المركبة المحدد");
            }
        }

        private void LogModelStateErrors()
        {
            foreach (var error in ModelState
                .Where(m => m.Value?.Errors.Count > 0)
                .SelectMany(m => m.Value!.Errors.Select(e => new { m.Key, e.ErrorMessage })))
            {
                Console.WriteLine($"ModelState error: {error.Key} - {error.ErrorMessage}");
            }
        }
    }
}
