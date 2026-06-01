using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models;
using VehicleContractSystem_Final.Services;

namespace VehicleContractSystem_Final.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/png"
        };

        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly IWebHostEnvironment _environment;

        public DocumentController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _environment = environment;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            int contractId,
            ContractDocumentType documentType,
            IFormFile? file,
            string? notes)
        {
            var contract = await _context.Contracts
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return NotFound();

            if (!await _currentUserService.CanAccessContract(contract))
                return Forbid();

            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction("Details", "Contract", new { id = contract.Id });
            }

            var extension = Path.GetExtension(file!.FileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "contracts", contract.Id.ToString());
            Directory.CreateDirectory(uploadRoot);

            var fullPath = Path.Combine(uploadRoot, storedFileName);
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads", "contracts", contract.Id.ToString(), storedFileName)
                .Replace('\\', '/');
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            var document = new ContractDocument
            {
                ContractId = contract.Id,
                VehicleId = contract.VehicleId,
                UploadedByUserId = currentUser?.Id,
                DocumentType = documentType,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                FilePath = relativePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Notes = (notes ?? string.Empty).Trim(),
                UploadedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.ContractDocuments.Add(document);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "رفع مستند",
                "ContractDocument",
                document.Id.ToString(),
                $"تم رفع مستند للعقد رقم {contract.ContractNumber}: {document.OriginalFileName}");

            TempData["SuccessMessage"] = "تم رفع المستند بنجاح";
            return RedirectToAction("Details", "Contract", new { id = contract.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.ContractDocuments
                .Include(d => d.Contract)
                    .ThenInclude(c => c.Payments)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            if (!await _currentUserService.CanAccessContract(document.Contract))
                return Forbid();

            var fullPath = BuildSafeDocumentPath(document);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return NotFound();

            await _auditService.LogAsync(
                "تنزيل مستند",
                "ContractDocument",
                document.Id.ToString(),
                $"تم تنزيل مستند للعقد رقم {document.Contract.ContractNumber}: {document.OriginalFileName}");

            return PhysicalFile(fullPath, document.ContentType, document.OriginalFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.ContractDocuments
                .Include(d => d.Contract)
                    .ThenInclude(c => c.Payments)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            if (!await _currentUserService.CanAccessContract(document.Contract))
                return Forbid();

            document.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "حذف مستند",
                "ContractDocument",
                document.Id.ToString(),
                $"تم حذف مستند من العقد رقم {document.Contract.ContractNumber}: {document.OriginalFileName}");

            TempData["SuccessMessage"] = "تم حذف المستند بنجاح";
            return RedirectToAction("Details", "Contract", new { id = document.ContractId });
        }

        private static string? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "يرجى اختيار ملف للرفع";

            if (file.Length > MaxFileSize)
                return "حجم الملف يجب ألا يتجاوز 5MB";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return "نوع الملف غير مسموح. الملفات المسموحة: PDF، JPG، PNG";

            if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
                return "نوع محتوى الملف غير مسموح";

            return null;
        }

        private string? BuildSafeDocumentPath(ContractDocument document)
        {
            if (document.ContractId <= 0 || string.IsNullOrWhiteSpace(document.StoredFileName))
                return null;

            var fileName = Path.GetFileName(document.StoredFileName);
            var uploadRoot = Path.GetFullPath(Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "contracts",
                document.ContractId.ToString()));
            var fullPath = Path.GetFullPath(Path.Combine(uploadRoot, fileName));

            return fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }
    }
}
