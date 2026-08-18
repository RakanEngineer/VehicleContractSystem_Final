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
        private readonly IAuditService _auditService;

        public ContractController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _auditService = auditService;
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
                .Include(c => c.Documents.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية الوصول لهذا العقد";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanCloseContract = await CanCloseContractAsync(contract);

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
            ModelState.Remove("Contract.Vehicle");
            ModelState.Remove("Contract.VehicleId");
            ModelState.Remove("Contract.VehicleModelId");
            ModelState.Remove("Contract.ColorId");
            ModelState.Remove("Contract.VehicleCategory");
            await ApplyVehicleLookupValuesAsync(contract);
            await ValidateVehicleLookupsAsync(contract);
            NormalizeManualVehicleModel(contract);
            NormalizeManualVehicleColor(contract);
            NormalizeVehicleIdentityFields(contract);
            ValidateCreateFinancialValues(viewModel);
            await ValidateVehicleIsNotBlockedForCreateAsync(contract);
            var existingVehicle = await FindVehicleForNewContractAsync(contract);
            await ValidateVehicleContractAvailabilityForCreateAsync(contract, existingVehicle);
            await ValidateLicenseNumberForCreateAsync(contract, existingVehicle);

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    contract.CreatedByUserId = currentUser.Id;
                    contract.OfficeId = currentUser.OfficeId.Value;
                    contract.CreatedAt = DateTime.Now;
                    contract.Status = ContractStatus.Open;

                    if (string.IsNullOrWhiteSpace(contract.ContractNumber))
                    {
                        contract.ContractNumber = GenerateContractNumber();
                    }

                    EnsureVerificationToken(contract);
                    await EnsureManualColorAsync(contract);
                    await ApplyVehicleLookupValuesAsync(contract);

                    var vehicle = await GetOrCreateVehicleForNewContractAsync(contract, existingVehicle);
                    contract.VehicleId = vehicle.Id;
                    ApplyVehicleSnapshotFields(contract, vehicle);
                    await ApplyVehicleStatusChangeAsync(
                        vehicle,
                        VehicleStatus.UnderContract,
                        "تحويل المركبة إلى عليها عقد",
                        $"تم تحويل المركبة رقم {vehicle.LicenseNumber} إلى حالة عليها عقد عند إنشاء العقد رقم {contract.ContractNumber}");

                    _context.Add(contract);
                    await _context.SaveChangesAsync();

                    if (viewModel.InitialPaidAmount > 0)
                    {
                        var initialPayment = new Payment
                        {
                            ContractId = contract.Id,
                            Amount = viewModel.InitialPaidAmount,
                            PaymentDate = DateTime.Now,
                            Notes = "دفعة أولية عند إنشاء العقد",
                            PaymentMethod = "نقداً",
                            RecordedByUserId = currentUser.Id,
                            CreatedAt = DateTime.Now
                        };

                        _context.Payments.Add(initialPayment);

                        await _context.SaveChangesAsync();

                        await _auditService.LogAsync(
                            "إضافة دفعة",
                            "Payment",
                            initialPayment.Id.ToString(),
                            $"تم تسجيل دفعة أولية بمبلغ {viewModel.InitialPaidAmount:N2} عند إنشاء العقد رقم {contract.ContractNumber}");
                    }

                    await _auditService.LogAsync(
                        "إنشاء عقد",
                        "Contract",
                        contract.Id.ToString(),
                        $"تم إنشاء العقد رقم {contract.ContractNumber}");
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "تم إنشاء العقد بنجاح";
                    return RedirectToAction(nameof(Details), new { id = contract.Id });
                }
                catch (DbUpdateException ex) when (AddDuplicateVehicleIdentityModelError(ex))
                {
                    await transaction.RollbackAsync();
                }
            }

            LogModelStateErrors();
            var rebuiltViewModel = await BuildContractViewModelAsync(contract);
            rebuiltViewModel.InitialPaidAmount = viewModel.InitialPaidAmount;

            return View(rebuiltViewModel);
        }

        // GET: Contract/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Vehicle)
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
                TempData["ErrorMessage"] = "العقد مغلق ولا يمكن تعديل بياناته القانونية";
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

            var existingContract = await _context.Contracts
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(c => c.Id == id);
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
                TempData["ErrorMessage"] = "العقد مغلق ولا يمكن تعديل بياناته القانونية";
                return RedirectToAction(nameof(Details), new { id = existingContract.Id });
            }

            contract.ContractNumber = existingContract.ContractNumber;

            // Remove validation for fields that are immutable or supplied by the persisted contract.
            ModelState.Remove("Contract.ContractNumber");
            ModelState.Remove("Contract.ContractDate");
            ModelState.Remove("Contract.CreatedByUser");
            ModelState.Remove("Contract.CreatedByUserId");
            ModelState.Remove("Contract.Office");
            ModelState.Remove("Contract.OfficeId");
            ModelState.Remove("Contract.Province");
            ModelState.Remove("Contract.City");
            ModelState.Remove("Contract.Payments");
            ModelState.Remove("Contract.Vehicle");
            ModelState.Remove("Contract.VehicleId");
            ModelState.Remove("Contract.Status");
            ModelState.Remove("Contract.CurrentOwnerName");
            ModelState.Remove("Contract.CurrentOwnerAddress");
            ModelState.Remove("Contract.AnnualRegistrationNumber");
            ModelState.Remove("Contract.VehicleBrand");
            ModelState.Remove("Contract.VehicleTypeId");
            ModelState.Remove("Contract.VehicleModelId");
            ModelState.Remove("Contract.ColorId");
            ModelState.Remove("Contract.VehicleCategory");
            ModelState.Remove("Contract.VehicleModel");
            ModelState.Remove("Contract.VehicleColor");
            ModelState.Remove("Contract.ManufactureYear");
            ModelState.Remove("Contract.ChassisNumber");
            ModelState.Remove("Contract.LicenseNumber");
            ModelState.Remove("Contract.VehicleTypeNameSnapshot");
            ModelState.Remove("Contract.VehicleModelNameSnapshot");
            ModelState.Remove("Contract.VehicleColorNameSnapshot");
            ModelState.Remove("Contract.LicenseNumberSnapshot");
            ModelState.Remove("Contract.ChassisNumberSnapshot");
            ModelState.Remove("Contract.ManufactureYearSnapshot");

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only non-vehicle fields. VehicleId and snapshot values are immutable after creation.
                    existingContract.SellerName = contract.SellerName;
                    existingContract.SellerAddress = contract.SellerAddress;
                    existingContract.SellerPhone = contract.SellerPhone;
                    existingContract.SellerIdNumber = contract.SellerIdNumber;
                    existingContract.BuyerName = contract.BuyerName;
                    existingContract.BuyerAddress = contract.BuyerAddress;
                    existingContract.BuyerPhone = contract.BuyerPhone;
                    existingContract.BuyerIdNumber = contract.BuyerIdNumber;
                    existingContract.VehiclePrice = contract.VehiclePrice;
                    existingContract.Notes = contract.Notes;
                    existingContract.LastModifiedAt = DateTime.Now;
                    existingContract.LastModifiedByUserId = (await _currentUserService.GetCurrentUserAsync())?.Id;

                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync(
                        "تعديل عقد",
                        "Contract",
                        existingContract.Id.ToString(),
                        $"تم تعديل العقد رقم {existingContract.ContractNumber}");

                    TempData["SuccessMessage"] = "تم تحديث العقد بنجاح";
                    return RedirectToAction(nameof(Details), new { id = existingContract.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(existingContract.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            LogModelStateErrors();
            CopyImmutableContractFields(existingContract, contract);
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
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return NotFound();

            // Check access permission
            if (!await _currentUserService.CanAccessContract(contract))
            {
                await LogFailedCloseAttemptAsync(contract, "محاولة إغلاق عقد بدون صلاحية وصول");
                TempData["ErrorMessage"] = "ليس لديك صلاحية إغلاق هذا العقد";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (!await CanCloseContractAsync(contract))
            {
                await LogFailedCloseAttemptAsync(contract, "محاولة إغلاق عقد بدون صلاحية إغلاق");
                TempData["ErrorMessage"] = await GetCloseContractDeniedMessageAsync(contract);
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (contract.Status != ContractStatus.Open)
            {
                await LogFailedCloseAttemptAsync(contract, "محاولة إغلاق عقد مغلق مسبقاً");
                TempData["ErrorMessage"] = "لا يمكن إغلاق عقد مغلق مسبقاً";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            if (contract.RemainingAmount > 0)
            {
                await LogFailedCloseAttemptAsync(contract, "محاولة إغلاق عقد قبل تسديد كامل المبلغ");
                TempData["ErrorMessage"] = "لا يمكن إغلاق العقد قبل تسديد كامل المبلغ";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }

            contract.Status = ContractStatus.Closed;
            contract.LastModifiedAt = DateTime.Now;
            contract.LastModifiedByUserId = (await _currentUserService.GetCurrentUserAsync())?.Id;
            if (contract.Vehicle != null)
            {
                await ApplyVehicleStatusChangeAsync(
                    contract.Vehicle,
                    VehicleStatus.Sold,
                    "تحويل المركبة إلى مباعة",
                    $"تم تحويل المركبة رقم {contract.Vehicle.LicenseNumber} إلى حالة مباعة بعد إغلاق العقد رقم {contract.ContractNumber}");
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                "إغلاق عقد",
                "Contract",
                contract.Id.ToString(),
                $"تم إغلاق العقد رقم {contract.ContractNumber}");

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

            await EnsureVerificationTokenAsync(contract);
            var verificationUrl = BuildVerificationUrl(contract);
            var qrCodeBase64 = QRCodeHelper.GenerateQRCode(verificationUrl);

            ViewBag.QRCodeBase64 = qrCodeBase64;
            ViewBag.VerificationUrl = verificationUrl;

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

            await EnsureVerificationTokenAsync(contract);
            var pdfBytes = GeneratePDF(contract, BuildVerificationUrl(contract));
            return File(pdfBytes, "application/pdf", $"Contract_{contract.ContractNumber}.pdf");
        }

        private byte[] GeneratePDF(Contract contract, string verificationUrl)
        {
            var vehicleTypeName = SnapshotOrLegacy(contract.VehicleTypeNameSnapshot, contract.VehicleCategory);
            var vehicleModelName = SnapshotOrLegacy(contract.VehicleModelNameSnapshot, contract.VehicleModel);
            var vehicleColorName = SnapshotOrLegacy(contract.VehicleColorNameSnapshot, contract.VehicleColor);
            var licenseNumber = SnapshotOrLegacy(contract.LicenseNumberSnapshot, contract.LicenseNumber);
            var chassisNumber = SnapshotOrLegacy(contract.ChassisNumberSnapshot, contract.ChassisNumber);
            var manufactureYear = contract.ManufactureYearSnapshot ?? contract.ManufactureYear;
            var vehicleProvinceName = contract.Province?.NameAr switch
            {
                "كربلاء" => "كربلاء المقدسة",
                "النجف" => "النجف الأشرف",
                _ => contract.Province?.NameAr
            };

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
            document.Add(new Paragraph($"صنف المركبة: {vehicleTypeName}", arabicFont));
            document.Add(new Paragraph($"نوع المركبة: {vehicleModelName}", arabicFont));
            document.Add(new Paragraph($"اللون: {vehicleColorName}", arabicFont));
            document.Add(new Paragraph($"سنة الصنع: {manufactureYear}", arabicFont));
            document.Add(new Paragraph($"رقم السنوية: {contract.AnnualRegistrationNumber}", arabicFont));
            document.Add(new Paragraph($"محافظة المركبة: {vehicleProvinceName}", arabicFont));
            document.Add(new Paragraph($"رقم المركبة: {licenseNumber}", arabicFont));
            document.Add(new Paragraph($"رقم الشاصي: {chassisNumber}", arabicFont));
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph("المعلومات المالية:", titleFont));
            document.Add(new Paragraph($"سعر المركبة: {Money(contract.VehiclePrice)}", arabicFont));
            document.Add(new Paragraph($"استلم البائع مبلغ وقدره: {Money(contract.PaidAmount)}", arabicFont));
            document.Add(new Paragraph($"المبلغ المتبقي: {Money(contract.RemainingAmount)}", arabicFont));

            var qrCodeBase64 = QRCodeHelper.GenerateQRCode(verificationUrl);

            var qrBytes = Convert.FromBase64String(qrCodeBase64);
            var qrImage = Image.GetInstance(qrBytes);
            qrImage.ScaleToFit(100f, 100f);
            qrImage.Alignment = Element.ALIGN_CENTER;
            document.Add(qrImage);

            document.Close();

            return ms.ToArray();
        }

        private static string SnapshotOrLegacy(string? snapshotValue, string legacyValue)
        {
            return string.IsNullOrWhiteSpace(snapshotValue) ? legacyValue : snapshotValue;
        }

        private static string Money(decimal value)
        {
            return $"${value:N0}";
        }

        private static void CopyImmutableContractFields(Contract source, Contract target)
        {
            target.ContractNumber = source.ContractNumber;
            target.VerificationToken = source.VerificationToken;
            target.ContractDate = source.ContractDate;
            target.CreatedAt = source.CreatedAt;
            target.CreatedByUserId = source.CreatedByUserId;
            target.OfficeId = source.OfficeId;
            target.ProvinceId = source.ProvinceId;
            target.Province = source.Province;
            target.Status = source.Status;
            target.VehicleId = source.VehicleId;
            target.Vehicle = source.Vehicle;
            target.CurrentOwnerName = source.CurrentOwnerName;
            target.CurrentOwnerAddress = source.CurrentOwnerAddress;
            target.AnnualRegistrationNumber = source.AnnualRegistrationNumber;
            target.VehicleBrand = source.VehicleBrand;
            target.VehicleTypeId = source.VehicleTypeId;
            target.VehicleModelId = source.VehicleModelId;
            target.ColorId = source.ColorId;
            target.VehicleCategory = source.VehicleCategory;
            target.VehicleModel = source.VehicleModel;
            target.VehicleColor = source.VehicleColor;
            target.LicenseNumber = source.LicenseNumber;
            target.ChassisNumber = source.ChassisNumber;
            target.ManufactureYear = source.ManufactureYear;
            target.VehicleTypeNameSnapshot = source.VehicleTypeNameSnapshot;
            target.VehicleModelNameSnapshot = source.VehicleModelNameSnapshot;
            target.VehicleColorNameSnapshot = source.VehicleColorNameSnapshot;
            target.LicenseNumberSnapshot = source.LicenseNumberSnapshot;
            target.ChassisNumberSnapshot = source.ChassisNumberSnapshot;
            target.ManufactureYearSnapshot = source.ManufactureYearSnapshot;
        }

        private async Task<bool> CanCloseContractAsync(Contract contract)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return false;

            if (await _currentUserService.IsSuperAdminAsync())
                return true;

            return await _currentUserService.IsOfficeAdminAsync()
                && currentUser.OfficeId == contract.OfficeId;
        }

        private async Task<string> GetCloseContractDeniedMessageAsync(Contract contract)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser != null &&
                await _currentUserService.IsOfficeAdminAsync() &&
                currentUser.OfficeId != contract.OfficeId)
            {
                return "لا يمكن إغلاق عقد غير تابع لمكتبك";
            }

            return "ليس لديك صلاحية إغلاق هذا العقد";
        }

        private async Task LogFailedCloseAttemptAsync(Contract contract, string reason)
        {
            await _auditService.LogAsync(
                "محاولة إغلاق مرفوضة",
                "Contract",
                contract.Id.ToString(),
                $"{reason} للعقد رقم {contract.ContractNumber}");
        }

        private string GenerateContractNumber()
        {
            var year = DateTime.Now.Year;
            var random = new Random();
            var number = random.Next(1000, 9999);
            return $"CT-{year}-{number}";
        }

        private static void EnsureVerificationToken(Contract contract)
        {
            if (string.IsNullOrWhiteSpace(contract.VerificationToken))
            {
                contract.VerificationToken = Guid.NewGuid().ToString("N");
            }
        }

        private async Task EnsureVerificationTokenAsync(Contract contract)
        {
            if (!string.IsNullOrWhiteSpace(contract.VerificationToken))
                return;

            contract.VerificationToken = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();
        }

        private string BuildVerificationUrl(Contract contract)
        {
            EnsureVerificationToken(contract);

            return $"{Request.Scheme}://{Request.Host}/Verify/Contract/{contract.VerificationToken}";
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
                    .OrderBy(v => v.Id)
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
            var color = contract.ColorId.HasValue && string.IsNullOrWhiteSpace(contract.VehicleColor)
                ? await _context.Colors.FindAsync(contract.ColorId.Value)
                : null;

            contract.VehicleCategory = vehicleType?.NameAr ?? string.Empty;
            contract.VehicleColor = !string.IsNullOrWhiteSpace(contract.VehicleColor)
                ? contract.VehicleColor.Trim()
                : color?.NameAr ?? string.Empty;
        }

        private async Task EnsureManualColorAsync(Contract contract)
        {
            var colorName = (contract.VehicleColor ?? string.Empty).Trim();

            var color = await _context.Colors
                .FirstOrDefaultAsync(c => c.NameAr == colorName);

            if (color == null)
            {
                color = new VehicleContractSystem_Final.Models.Color
                {
                    NameAr = colorName,
                    NameEn = colorName
                };

                _context.Colors.Add(color);
                await _context.SaveChangesAsync();
            }

            contract.ColorId = color.Id;
            contract.VehicleColor = color.NameAr;
        }

        private async Task ValidateVehicleLookupsAsync(Contract contract)
        {
            if (string.IsNullOrWhiteSpace(contract.VehicleColor))
            {
                ModelState.AddModelError("Contract.VehicleColor", "يرجى إدخال لون المركبة");
            }

            if (!contract.VehicleTypeId.HasValue || !await _context.VehicleTypes.AnyAsync(t => t.Id == contract.VehicleTypeId.Value))
                ModelState.AddModelError("Contract.VehicleTypeId", "يرجى اختيار صنف مركبة صحيح");

            if (string.IsNullOrWhiteSpace(contract.VehicleModel))
                ModelState.AddModelError("Contract.VehicleModel", "يرجى إدخال نوع المركبة");
        }

        private void ValidateCreateFinancialValues(ContractViewModel viewModel)
        {
            if (viewModel.Contract.VehiclePrice <= 0)
            {
                ModelState.AddModelError("Contract.VehiclePrice", "يرجى إدخال سعر مركبة أكبر من صفر");
            }

            if (viewModel.InitialPaidAmount < 0)
            {
                ModelState.AddModelError(nameof(viewModel.InitialPaidAmount), "المبلغ المستلم يجب أن يكون صفراً أو أكثر");
            }

            if (viewModel.InitialPaidAmount > viewModel.Contract.VehiclePrice)
            {
                ModelState.AddModelError(nameof(viewModel.InitialPaidAmount), "المبلغ المستلم لا يمكن أن يكون أكبر من سعر المركبة");
            }
        }

        private static void NormalizeManualVehicleModel(Contract contract)
        {
            contract.VehicleModel = (contract.VehicleModel ?? string.Empty).Trim();
        }

        private static void NormalizeManualVehicleColor(Contract contract)
        {
            contract.VehicleColor = (contract.VehicleColor ?? string.Empty).Trim();
        }

        private static void NormalizeVehicleIdentityFields(Contract contract)
        {
            contract.ChassisNumber = (contract.ChassisNumber ?? string.Empty).Trim();
            contract.LicenseNumber = (contract.LicenseNumber ?? string.Empty).Trim();
        }

        private async Task ValidateVehicleContractAvailabilityForCreateAsync(Contract contract, Vehicle? existingVehicle)
        {
            if (string.IsNullOrWhiteSpace(contract.ChassisNumber) && existingVehicle == null)
                return;

            if (existingVehicle != null &&
                (existingVehicle.Status == VehicleStatus.UnderContract ||
                 existingVehicle.Status == VehicleStatus.Reserved ||
                 existingVehicle.Status == VehicleStatus.Suspended))
            {
                ModelState.AddModelError("Contract.ChassisNumber", GetVehicleStatusBlockMessage(existingVehicle.Status));
                return;
            }

            var hasOpenContract = await _context.Contracts.AnyAsync(c =>
                c.Status == ContractStatus.Open &&
                ((existingVehicle != null && c.VehicleId == existingVehicle.Id) ||
                 (!string.IsNullOrWhiteSpace(contract.ChassisNumber) && c.ChassisNumber == contract.ChassisNumber)));

            if (hasOpenContract)
            {
                ModelState.AddModelError("Contract.ChassisNumber", "توجد معاملة مفتوحة لهذه المركبة ولا يمكن إنشاء عقد جديد قبل إغلاق العقد السابق");
            }
        }

        private static string GetVehicleStatusBlockMessage(VehicleStatus status)
        {
            return status switch
            {
                VehicleStatus.UnderContract => "توجد معاملة مفتوحة لهذه المركبة ولا يمكن إنشاء عقد جديد قبل إغلاق العقد السابق",
                VehicleStatus.Reserved => "لا يمكن إنشاء عقد جديد لأن المركبة محجوزة حالياً",
                VehicleStatus.Suspended => "لا يمكن إنشاء عقد جديد لأن المركبة موقوفة حالياً",
                _ => "لا يمكن إنشاء عقد جديد لهذه المركبة في حالتها الحالية"
            };
        }

        private async Task ValidateVehicleIsNotBlockedForCreateAsync(Contract contract)
        {
            if (string.IsNullOrWhiteSpace(contract.ChassisNumber) &&
                string.IsNullOrWhiteSpace(contract.LicenseNumber))
            {
                return;
            }

            var blockedVehicle = await _context.BlockedVehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.IsActive &&
                    ((!string.IsNullOrWhiteSpace(contract.ChassisNumber) && b.ChassisNumber == contract.ChassisNumber) ||
                     (!string.IsNullOrWhiteSpace(contract.LicenseNumber) && b.LicenseNumber == contract.LicenseNumber)));

            if (blockedVehicle == null)
                return;

            const string message = "لا يمكن إنشاء عقد لهذه المركبة لوجود منع أو بلاغ عليها";
            ModelState.AddModelError("Contract.ChassisNumber", message);

            await _auditService.LogAsync(
                "رفض عقد لمركبة ممنوعة",
                "BlockedVehicle",
                blockedVehicle.Id.ToString(),
                $"تم رفض إنشاء عقد لمركبة ممنوعة. الشاصي: {contract.ChassisNumber}، الرقم: {contract.LicenseNumber}، السبب: {blockedVehicle.Reason}");
        }

        private async Task ValidateLicenseNumberForCreateAsync(Contract contract, Vehicle? existingVehicle)
        {
            if (string.IsNullOrWhiteSpace(contract.LicenseNumber))
                return;

            var vehicleWithLicense = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.LicenseNumber == contract.LicenseNumber);

            if (vehicleWithLicense == null)
                return;

            if (existingVehicle != null && vehicleWithLicense.Id == existingVehicle.Id)
                return;

            ModelState.AddModelError("Contract.LicenseNumber", "رقم المركبة مستخدم مسبقاً");
        }

        private bool AddDuplicateVehicleIdentityModelError(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;

            if (message.Contains("IX_Vehicles_ChassisNumber", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("ChassisNumber", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Contract.ChassisNumber", "توجد معاملة مفتوحة لهذه المركبة ولا يمكن إنشاء عقد جديد قبل إغلاق العقد السابق");
                return true;
            }

            if (message.Contains("IX_Vehicles_LicenseNumber", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("LicenseNumber", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Contract.LicenseNumber", "رقم المركبة مستخدم مسبقاً");
                return true;
            }

            return false;
        }

        private async Task<Vehicle> GetOrCreateVehicleForNewContractAsync(Contract contract, Vehicle? existingVehicle = null)
        {
            existingVehicle ??= await FindVehicleForNewContractAsync(contract);
            if (existingVehicle != null)
                return existingVehicle;

            var vehicle = new Vehicle
            {
                OwnerId = null,
                VehicleTypeId = contract.VehicleTypeId!.Value,
                VehicleModelId = null,
                ColorId = contract.ColorId!.Value,
                LicenseNumber = contract.LicenseNumber,
                ChassisNumber = contract.ChassisNumber,
                ManufactureYear = contract.ManufactureYear,
                VehicleBrand = contract.VehicleBrand,
                VehicleModelName = contract.VehicleModel,
                VehicleColorName = contract.VehicleColor,
                VehicleCategory = contract.VehicleCategory,
                NumberOfWheels = 4,
                Status = VehicleStatus.Available
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return vehicle;
        }

        private async Task ApplyVehicleStatusChangeAsync(
            Vehicle vehicle,
            VehicleStatus newStatus,
            string action,
            string description)
        {
            if (vehicle.Status == newStatus)
                return;

            var oldStatus = vehicle.Status;
            vehicle.Status = newStatus;

            await _auditService.LogAsync(
                action,
                "Vehicle",
                vehicle.Id.ToString(),
                $"{description}. الحالة السابقة: {GetVehicleStatusLabel(oldStatus)}");
        }

        private static string GetVehicleStatusLabel(VehicleStatus status)
        {
            return status switch
            {
                VehicleStatus.Available => "متاحة",
                VehicleStatus.UnderContract => "عليها عقد",
                VehicleStatus.Sold => "مباعة",
                VehicleStatus.Reserved => "محجوزة",
                VehicleStatus.Suspended => "موقوفة",
                _ => status.ToString()
            };
        }

        private async Task<Vehicle?> FindVehicleForNewContractAsync(Contract contract)
        {
            if (!string.IsNullOrWhiteSpace(contract.ChassisNumber))
            {
                return await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.ChassisNumber == contract.ChassisNumber);
            }

            return null;
        }

        private static void ApplyVehicleSnapshotFields(Contract contract, Vehicle vehicle)
        {
            contract.VehicleTypeNameSnapshot = contract.VehicleCategory;
            contract.VehicleModelNameSnapshot = contract.VehicleModel;
            contract.VehicleColorNameSnapshot = contract.VehicleColor;
            contract.LicenseNumberSnapshot = !string.IsNullOrWhiteSpace(contract.LicenseNumber)
                ? contract.LicenseNumber
                : vehicle.LicenseNumber;
            contract.ChassisNumberSnapshot = !string.IsNullOrWhiteSpace(contract.ChassisNumber)
                ? contract.ChassisNumber
                : vehicle.ChassisNumber;
            contract.ManufactureYearSnapshot = contract.ManufactureYear > 0
                ? contract.ManufactureYear
                : vehicle.ManufactureYear;
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
