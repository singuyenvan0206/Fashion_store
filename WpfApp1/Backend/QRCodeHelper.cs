using System;
using System.Windows.Media.Imaging;
using QRCoder;
using System.IO;
using System.Windows.Media;

namespace WpfApp1
{
    public static class QRCodeHelper
    {
        /// <summary>
        /// Tạo QR code cho chuyển tiền ngân hàng
        /// </summary>
        /// <param name="bankAccount">Số tài khoản ngân hàng</param>
        /// <param name="bankName">Tên ngân hàng</param>
        /// <param name="accountName">Tên chủ tài khoản</param>
        /// <param name="amount">Số tiền cần chuyển</param>
        /// <param name="content">Nội dung chuyển tiền</param>
        /// <returns>BitmapSource của QR code</returns>
        public static BitmapSource GenerateBankTransferQR(string bankAccount, string bankName, string accountName, decimal amount, string content)
        {
            // Tạo nội dung QR theo format VietQR
            string qrContent = $"2|99|{bankAccount}|{accountName}|{bankName}|{amount:F0}|{content}";
            
            return GenerateQRCode(qrContent);
        }

        /// <summary>
        /// Tạo QR code cho chuyển tiền MoMo
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại MoMo</param>
        /// <param name="amount">Số tiền cần chuyển</param>
        /// <param name="content">Nội dung chuyển tiền</param>
        /// <returns>BitmapSource của QR code</returns>
        public static BitmapSource GenerateMoMoQR(string phoneNumber, decimal amount, string content)
        {
            // Tạo nội dung QR cho MoMo
            string qrContent = $"2|99|{phoneNumber}|{amount:F0}|{content}";
            
            return GenerateQRCode(qrContent);
        }

        /// <summary>
        /// Tạo QR code cho chuyển tiền ZaloPay
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại ZaloPay</param>
        /// <param name="amount">Số tiền cần chuyển</param>
        /// <param name="content">Nội dung chuyển tiền</param>
        /// <returns>BitmapSource của QR code</returns>
        public static BitmapSource GenerateZaloPayQR(string phoneNumber, decimal amount, string content)
        {
            // Tạo nội dung QR cho ZaloPay
            string qrContent = $"2|99|{phoneNumber}|{amount:F0}|{content}";
            
            return GenerateQRCode(qrContent);
        }

        /// <summary>
        /// Tạo QR code đơn giản với nội dung tùy chỉnh
        /// </summary>
        /// <param name="content">Nội dung QR code</param>
        /// <returns>BitmapSource của QR code</returns>
        public static BitmapSource GenerateQRCode(string content)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
                    {
                        using (QRCode qrCode = new QRCode(qrCodeData))
                        {
                            using (var qrCodeImage = qrCode.GetGraphic(20))
                            {
                                return ConvertToBitmapSource(qrCodeImage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating QR code: {ex.Message}");
                return CreateErrorQRCode();
            }
        }

        /// <summary>
        /// Chuyển đổi System.Drawing.Bitmap thành BitmapSource
        /// </summary>
        private static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                
                return bitmapImage;
            }
        }

        /// <summary>
        /// Tạo QR code lỗi khi không thể tạo QR code thật
        /// </summary>
        private static BitmapSource CreateErrorQRCode()
        {
            // Tạo một hình ảnh đơn giản thay thế
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

        /// <summary>
        /// Tạo QR code cho hóa đơn với thông tin thanh toán
        /// </summary>
        /// <param name="invoiceId">ID hóa đơn</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="paymentMethod">Phương thức thanh toán (Bank, MoMo, ZaloPay)</param>
        /// <param name="paymentInfo">Thông tin thanh toán (số tài khoản, số điện thoại, v.v.)</param>
        /// <returns>BitmapSource của QR code</returns>
        public static BitmapSource GenerateInvoicePaymentQR(int invoiceId, decimal amount, string paymentMethod, string paymentInfo)
        {
            string content = $"Hóa đơn #{invoiceId} - {amount:F0} VND";
            
            return paymentMethod.ToLower() switch
            {
                "momo" => GenerateMoMoQR(paymentInfo, amount, content),
                "zalopay" => GenerateZaloPayQR(paymentInfo, amount, content),
                "bank" => GenerateBankTransferQR(paymentInfo, "Ngân hàng", "Công ty ABC", amount, content),
                _ => GenerateQRCode($"Thanh toán hóa đơn #{invoiceId}: {amount:F0} VND")
            };
        }
    }
}
