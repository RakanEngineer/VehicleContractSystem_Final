using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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

        private static readonly IReadOnlyDictionary<string, string> AllowedContentTypesByExtension =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly IFileStorage _fileStorage;

        public DocumentController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            IFileStorage fileStorage)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _fileStorage = fileStorage;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSize)]
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

            var validationResult = await ValidateAndReadFileAsync(file);
            if (!validationResult.IsValid)
            {
                TempData["ErrorMessage"] = validationResult.ErrorMessage;
                return RedirectToAction("Details", "Contract", new { id = contract.Id });
            }

            validationResult.Content.Position = 0;
            var storedFile = await _fileStorage.SaveContractDocumentAsync(
                contract.Id,
                validationResult.Content,
                validationResult.Extension);
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            var document = new ContractDocument
            {
                ContractId = contract.Id,
                VehicleId = contract.VehicleId,
                UploadedByUserId = currentUser?.Id,
                DocumentType = documentType,
                OriginalFileName = Path.GetFileName(file!.FileName),
                StoredFileName = storedFile.StoredFileName,
                FilePath = storedFile.FilePath,
                ContentType = validationResult.ContentType,
                Sha256Hash = validationResult.Sha256Hash,
                StorageProvider = _fileStorage.ProviderName,
                MalwareScanStatus = "Pending",
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
                $"تم رفع مستند للعقد رقم {contract.ContractNumber}: {document.OriginalFileName}، SHA-256: {document.Sha256Hash}");

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

            var storedFile = await _fileStorage.OpenContractDocumentAsync(
                document.ContractId,
                document.StoredFileName,
                document.FilePath);
            if (storedFile == null)
                return NotFound();

            await _auditService.LogAsync(
                "تنزيل مستند",
                "ContractDocument",
                document.Id.ToString(),
                $"تم تنزيل مستند للعقد رقم {document.Contract.ContractNumber}: {document.OriginalFileName}");

            return File(storedFile.Content, document.ContentType, document.OriginalFileName);
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

        private static async Task<FileValidationResult> ValidateAndReadFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return FileValidationResult.Invalid("يرجى اختيار ملف للرفع");

            if (file.Length > MaxFileSize)
                return FileValidationResult.Invalid("حجم الملف يجب ألا يتجاوز 5MB");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return FileValidationResult.Invalid("نوع الملف غير مسموح. الملفات المسموحة: PDF، JPG، PNG");

            await using var input = file.OpenReadStream();
            var content = new MemoryStream();
            await input.CopyToAsync(content);

            if (content.Length != file.Length || content.Length > MaxFileSize)
                return FileValidationResult.Invalid("حجم الملف غير صالح");

            content.Position = 0;
            if (!TryDetectContentType(content, extension, out var detectedContentType))
                return FileValidationResult.Invalid("محتوى الملف لا يطابق نوع الملف المسموح");

            if (!AllowedContentTypesByExtension.TryGetValue(extension, out var expectedContentType) ||
                !string.Equals(expectedContentType, detectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                return FileValidationResult.Invalid("امتداد الملف لا يطابق محتواه الفعلي");
            }

            content.Position = 0;
            var sha256Hash = Convert.ToHexString(await SHA256.HashDataAsync(content)).ToLowerInvariant();
            content.Position = 0;

            return FileValidationResult.Valid(
                content,
                extension.ToLowerInvariant(),
                detectedContentType,
                sha256Hash);
        }

        private static bool TryDetectContentType(Stream content, string extension, out string contentType)
        {
            contentType = string.Empty;
            Span<byte> header = stackalloc byte[8];
            content.Position = 0;
            var bytesRead = content.Read(header);
            content.Position = 0;

            if (bytesRead >= 5 &&
                header[0] == 0x25 &&
                header[1] == 0x50 &&
                header[2] == 0x44 &&
                header[3] == 0x46 &&
                header[4] == 0x2D)
            {
                contentType = "application/pdf";
                return true;
            }

            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                contentType = "image/jpeg";
                return true;
            }

            if (bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                contentType = "image/png";
                return true;
            }

            return false;
        }

        private sealed class FileValidationResult
        {
            public bool IsValid { get; private init; }
            public string ErrorMessage { get; private init; } = string.Empty;
            public MemoryStream Content { get; private init; } = new();
            public string Extension { get; private init; } = string.Empty;
            public string ContentType { get; private init; } = string.Empty;
            public string Sha256Hash { get; private init; } = string.Empty;

            public static FileValidationResult Invalid(string errorMessage)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = errorMessage
                };
            }

            public static FileValidationResult Valid(
                MemoryStream content,
                string extension,
                string contentType,
                string sha256Hash)
            {
                return new FileValidationResult
                {
                    IsValid = true,
                    Content = content,
                    Extension = extension,
                    ContentType = contentType,
                    Sha256Hash = sha256Hash
                };
            }
        }
    }
}
