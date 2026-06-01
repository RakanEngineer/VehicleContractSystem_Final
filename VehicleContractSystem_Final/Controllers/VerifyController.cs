using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Data;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models.ViewModel;

namespace VehicleContractSystem_Final.Controllers
{
    [AllowAnonymous]
    public class VerifyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VerifyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Verify/Contract/{token:guid}")]
        public async Task<IActionResult> Contract(Guid token)
        {
            if (token == Guid.Empty)
                return View(BuildInvalidVerificationResult());

            var normalizedToken = token.ToString("N");
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Office)
                .FirstOrDefaultAsync(c => c.VerificationToken == normalizedToken);

            if (contract == null)
                return View(BuildInvalidVerificationResult());

            var chassisNumber = SnapshotOrLegacy(contract.ChassisNumberSnapshot, contract.ChassisNumber).Trim();
            var licenseNumber = SnapshotOrLegacy(contract.LicenseNumberSnapshot, contract.LicenseNumber).Trim();
            var hasComplianceWarning = await _context.BlockedVehicles
                .AsNoTracking()
                .AnyAsync(b => b.IsActive &&
                    ((!string.IsNullOrWhiteSpace(chassisNumber) && b.ChassisNumber == chassisNumber) ||
                     (!string.IsNullOrWhiteSpace(licenseNumber) && b.LicenseNumber == licenseNumber)));

            var viewModel = new ContractVerificationViewModel
            {
                IsValid = true,
                ContractNumber = contract.ContractNumber,
                OfficeName = contract.Office?.Name ?? "غير محدد",
                ContractDate = contract.ContractDate,
                ContractStatus = GetContractStatusLabel(contract.Status),
                VerificationStatus = GetVerificationStatusLabel(contract.Status),
                ChassisNumber = chassisNumber,
                LicenseNumber = licenseNumber,
                VehicleModel = SnapshotOrLegacy(contract.VehicleModelNameSnapshot, contract.VehicleModel),
                HasComplianceWarning = hasComplianceWarning
            };

            return View(viewModel);
        }

        private static ContractVerificationViewModel BuildInvalidVerificationResult()
        {
            return new ContractVerificationViewModel
            {
                IsValid = false,
                Message = "العقد غير موجود أو غير صالح",
                VerificationStatus = "غير صالح"
            };
        }

        private static string SnapshotOrLegacy(string? snapshotValue, string legacyValue)
        {
            return string.IsNullOrWhiteSpace(snapshotValue) ? legacyValue : snapshotValue;
        }

        private static string GetContractStatusLabel(ContractStatus status)
        {
            return status switch
            {
                ContractStatus.Open => "مفتوح",
                ContractStatus.Closed => "مغلق",
                _ => "ملغي مستقبلاً"
            };
        }

        private static string GetVerificationStatusLabel(ContractStatus status)
        {
            return status switch
            {
                ContractStatus.Open => "صالح",
                ContractStatus.Closed => "مغلق",
                _ => "ملغي مستقبلاً"
            };
        }
    }
}
