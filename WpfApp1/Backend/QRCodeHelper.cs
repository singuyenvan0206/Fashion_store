using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Threading;

namespace WpfApp1
{
    public static class QRCodeHelper
    {
        private static readonly HttpClient _http = new HttpClient();

        public static BitmapSource GenerateVietQRCode_Safe(string bankCode, string bankAccount, decimal amount, string description = "", bool includeQueryDownload = false, int size = 370, string accountHolder = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bankCode) || string.IsNullOrWhiteSpace(bankAccount))
                    return CreateErrorQRCode("Thiếu thông tin ngân hàng", size);

                // Sanitize
                bankCode = bankCode.Trim().ToLowerInvariant();
                bankAccount = bankAccount.Replace(" ", string.Empty).Trim();

                var amountInt = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
                if (amountInt <= 0) return CreateErrorQRCode("Chưa có sản phẩm hoặc tổng tiền = 0", size);

                // Sanitize and format description to avoid confusion with amounts
                var desc = NormalizeDescription(description);

                // Build URL safely using string interpolation of escaped segments
                var baseUrl = "https://vietqr.co/api/generate";
                
                // VietQR API format: /{bankCode}/{bankAccount}/{accountHolder}/{amount}/{description}
                // If account holder is provided, include it; otherwise skip it
                string url;
                if (!string.IsNullOrWhiteSpace(accountHolder))
                {
                    url = $"{baseUrl}/{Uri.EscapeDataString(bankCode)}/{Uri.EscapeDataString(bankAccount)}/{Uri.EscapeDataString(accountHolder)}/{Uri.EscapeDataString(amountInt.ToString())}/{Uri.EscapeDataString(desc)}";
                }
                else
                {
                    // If no account holder, use old format for compatibility
                    url = $"{baseUrl}/{Uri.EscapeDataString(bankCode)}/{Uri.EscapeDataString(bankAccount)}/{Uri.EscapeDataString(amountInt.ToString())}/{Uri.EscapeDataString(desc)}";
                }

                // Build query string (keep minimal). Avoid 'download' unless you need it.
                var queryParts = new List<string>
                {
                    "isMask=1",
                    "logo=1",
                    "style=2",
                    "bg=61"
                };
                
                if (includeQueryDownload)
                    queryParts.Add("download=1");

                if (queryParts.Count > 0)
                    url += "?" + string.Join("&", queryParts);

                // Request with timeout
                using (var cts = new System.Threading.CancellationTokenSource(10000)) // 10 seconds timeout
                {
                    var resp = _http.GetAsync(url, cts.Token).GetAwaiter().GetResult();

                    // Check for specific VietQR API errors (523 is "Origin Unreachable")
                    if (!resp.IsSuccessStatusCode || resp.StatusCode == (System.Net.HttpStatusCode)523)
                    {
                        return GenerateFallbackQRCode(bankCode, bankAccount, amountInt, desc, size, accountHolder);
                    }

                    var respBody = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                    using (var ms = new MemoryStream(respBody))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        return bitmap;
                    }
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                return CreateErrorQRCode("Timeout", size);
            }
            catch (Exception)
            {
                return CreateErrorQRCode("Lỗi tạo QR", size);
            }
        }

        private static BitmapSource GenerateFallbackQRCode(string bankCode, string bankAccount, long amount, string description, int size, string accountHolder = "")
        {
            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw white background with blue border (indicating fallback)
                    drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.RoyalBlue, 3), new Rect(0, 0, size, size));

                    // Create payment information text
                    string paymentInfo;
                    if (amount <= 0)
                    {
                        paymentInfo = $"Bank: {bankCode.ToUpper()}\nAccount: {bankAccount}\nKHÔNG THỂ TẠO QR\nVui lòng thêm sản phẩm\nTổng tiền: 0₫";
                    }
                    else
                    {
                        var accountHolderInfo = !string.IsNullOrWhiteSpace(accountHolder) ? $"\nHolder: {accountHolder}" : "";
                        paymentInfo = $"Bank: {bankCode.ToUpper()}\nAccount: {bankAccount}{accountHolderInfo}\nAmount: {amount:N0}₫\nDesc: {description}";
                    }

                    // Draw text in the center
                    var formattedText = new FormattedText(
                        paymentInfo,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        Math.Min(12, size / 20.0),
                        Brushes.RoyalBlue,
                        VisualTreeHelper.GetDpi(Application.Current?.MainWindow ?? new System.Windows.Window()).PixelsPerDip);

                    // Center the text
                    double x = (size - formattedText.Width) / 2;
                    double y = (size - formattedText.Height) / 2;

                    drawingContext.DrawText(formattedText, new Point(x, y));

                    // Add corner markers to make it look more like a QR code
                    double cornerSize = size / 8;
                    var cornerBrush = Brushes.RoyalBlue;

                    // Top-left corner
                    drawingContext.DrawRectangle(cornerBrush, new Pen(Brushes.RoyalBlue, 2),
                        new Rect(10, 10, cornerSize, cornerSize));

                    // Top-right corner
                    drawingContext.DrawRectangle(cornerBrush, new Pen(Brushes.RoyalBlue, 2),
                        new Rect(size - cornerSize - 10, 10, cornerSize, cornerSize));

                    // Bottom-left corner
                    drawingContext.DrawRectangle(cornerBrush, new Pen(Brushes.RoyalBlue, 2),
                        new Rect(10, size - cornerSize - 10, cornerSize, cornerSize));
                }

                var renderTargetBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);

                return renderTargetBitmap;
            }
            catch (Exception)
            {
                return CreateErrorQRCode("Fallback Error", size);
            }
        }

        private static string NormalizeDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return "INVOICE";

            // Remove non-alphanumeric characters
            var desc = System.Text.RegularExpressions.Regex.Replace(description, @"[^a-zA-Z0-9]", "");

            if (string.IsNullOrWhiteSpace(desc)) return "INVOICE";

            // Ensure it starts with a letter to avoid confusion with amounts
            if (!char.IsLetter(desc[0]))
            {
                desc = "INV" + desc;
            }

            // Limit to 10 characters
            if (desc.Length > 10)
            {
                desc = desc.Substring(0, 10);
            }

            return desc;
        }

        private static BitmapSource CreateErrorQRCode(string message = "Lỗi", int size = 200)
        {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Draw white background with red border
                drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Red, 2), new Rect(0, 0, size, size));

                // Draw error text
                var formattedText = new FormattedText(
                    message,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    Math.Min(14, size / 12.0), // Adjust font size based on QR size
                    Brushes.Red,
                    VisualTreeHelper.GetDpi(Application.Current?.MainWindow ?? new System.Windows.Window()).PixelsPerDip);

                // Center the text
                double x = (size - formattedText.Width) / 2;
                double y = (size - formattedText.Height) / 2;

                drawingContext.DrawText(formattedText, new Point(x, y));
            }

            var renderTargetBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            return renderTargetBitmap;
        }
    }
}