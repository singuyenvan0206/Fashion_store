using System;
using System.Windows.Media.Imaging;
using QRCoder;
using System.IO;
using System.Windows.Media;

namespace WpfApp1
{
    public static class QRCodeHelper
    {
        public static readonly string TPBANK_QR = "0002010102111531397007040052044600000000000313738550010A000000727012500069704230111000000031370208QRIBFTTA5204513753037045802VN5913NGUYEN VAN SI6006Ha Noi8707CLASSIC63042CCE";

        public static readonly string MOMO_QR = "00020101021138620010A00000072701320006970454011899MM23363M811532870208QRIBFTTA53037045802VN62190515MOMOW2W8115328763042872 ";




        public static BitmapSource GeneratePaymentQR(string qrCodeString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrCodeString))
                {
                    System.Diagnostics.Debug.WriteLine("Payment QR code string is null or empty");
                    return CreateErrorQRCode();
                }

                System.Diagnostics.Debug.WriteLine($"Generating payment QR from string: {qrCodeString}");

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeString, QRCodeGenerator.ECCLevel.Q))
                using (QRCode qrCode = new QRCode(qrCodeData))
                using (var qrCodeImage = qrCode.GetGraphic(20))
                {
                    if (qrCodeImage == null)
                    {
                        System.Diagnostics.Debug.WriteLine("QR code image is null");
                        return CreateErrorQRCode();
                    }

                    return ConvertToBitmapSource(qrCodeImage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating payment QR: {ex.Message}");
                return CreateErrorQRCode();
            }
        }

        private static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            try
            {
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting bitmap: {ex.Message}");
                return CreateErrorQRCode();
            }
        }


        public static BitmapSource GenerateTPBankQR()
        {
            return GeneratePaymentQR(TPBANK_QR);
        }

        public static BitmapSource GenerateMoMoQR()
        {
            return GeneratePaymentQR(MOMO_QR);
        }

        public static BitmapSource GenerateQRByMethod(string paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "tpbank" or "bank" => GenerateTPBankQR(),
                "momo" => GenerateMoMoQR(),
                _ => GenerateTPBankQR()
            };
        }

        private static BitmapSource CreateErrorQRCode()
        {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 2), new System.Windows.Rect(0, 0, 200, 200));
                drawingContext.DrawText(
                    new FormattedText("QR Code\nError",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Arial"), 12, Brushes.Black, 96),
                    new System.Windows.Point(50, 90));
            }

            var renderTargetBitmap = new RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            renderTargetBitmap.Freeze();

            return renderTargetBitmap;
        }
    }
}