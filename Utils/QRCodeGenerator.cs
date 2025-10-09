//Utility card for the QR code generation
using System;
using QRCoder;

namespace EVChargingSystem.WebAPI.Utils
{
    public static class QRCodeGeneratorUtil
    {
        // Generates a QR code image and returns it as a Base64 string
        public static string GenerateQRCodeBase64(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentNullException(nameof(payload), "QR Code payload cannot be empty.");
            }

            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                // Create the QR Code data structure
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);

                // Use PngByteQRCode to generate a byte array
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    // Correct method name: GetGraphic
                    byte[] qrCodeAsPng = qrCode.GetGraphic(20);

                    // Convert to Base64
                    return Convert.ToBase64String(qrCodeAsPng);
                }
            }
        }
    }
}
