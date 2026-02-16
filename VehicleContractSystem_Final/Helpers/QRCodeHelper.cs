using QRCoder;

namespace VehicleContractSystem_Final.Helpers
{
    public static class QRCodeHelper
    {
        public static string GenerateQRCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeImage = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeImage);
        }

        public static string GenerateQRCodeForContract(string contractNumber, DateTime contractDate, string sellerName, string buyerName)
        {
            var contractInfo = $"Contract: {contractNumber}\nDate: {contractDate:yyyy-MM-dd}\nSeller: {sellerName}\nBuyer: {buyerName}";
            return GenerateQRCode(contractInfo);
        }
    }
}
