using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    public partial class InvoiceManagementWindow : Window
    {
        private readonly List<InvoiceItemViewModel> _invoiceItems = new();

        public InvoiceManagementWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            LoadCustomers();
            LoadProducts();
            ClearInvoice();
            UpdateTotals();
            InitializeQRCode();
            FocusProductEntry();
        }

        private void InitializeQRCode()
        {
            try
            {
                var paymentSettings = PaymentSettingsManager.Load();
                if (InvoiceQRCode != null)
                {
                    HideQRCode(); // Hidden by default until payment method selected
                }
            }
            catch
            {
                // Silent failure - QR code will be hidden
            }
        }

        private void FocusProductEntry()
        {
            ProductComboBox?.Focus();
        }

        #region Customer Management

        private void LoadCustomers()
        {
            try
            {
                var customers = DatabaseHelper.GetAllCustomers();
                var customerList = customers.ConvertAll(c => new CustomerListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone
                });

                CustomerComboBox.ItemsSource = customerList;

                if (customerList.Count > 0)
                {
                    CustomerComboBox.SelectedIndex = 0;
                }

                UpdateLoyaltyDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLoyaltyDisplay();
            UpdateTotals();
        }

        private void UpdateLoyaltyDisplay()
        {
            try
            {
                var (tier, points) = GetSelectedCustomerLoyalty();

                if (LoyaltyTierTextBlock != null)
                    LoyaltyTierTextBlock.Text = tier;

                if (LoyaltyPointsTextBlock != null)
                    LoyaltyPointsTextBlock.Text = $"{points} điểm";
            }
            catch
            {
                // Silent failure
            }
        }

        private (string tier, int points) GetSelectedCustomerLoyalty()
        {
            if (CustomerComboBox?.SelectedValue is int customerId && customerId > 0)
            {
                return DatabaseHelper.GetCustomerLoyalty(customerId);
            }
            return ("Regular", 0);
        }

        #endregion

        #region Product Management

        private void LoadProducts()
        {
            try
            {
                var products = DatabaseHelper.GetAllProducts();
                var productList = products.ConvertAll(p => new ProductListItem
                {
                    Id = p.Id,
                    Name = string.IsNullOrWhiteSpace(p.Code) ? p.Name : $"{p.Name} ({p.Code})",
                    UnitPrice = p.SalePrice,
                    StockQuantity = p.StockQuantity
                });

                ProductComboBox.ItemsSource = productList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductListItem selectedProduct)
            {
                UnitPriceTextBox.Text = selectedProduct.UnitPrice.ToString("F2");
                QuantityTextBox.Text = string.IsNullOrWhiteSpace(QuantityTextBox.Text) ? "1" : QuantityTextBox.Text;
            }
            UpdateTotals();
        }

        #endregion

        #region Invoice Item Management

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateProductSelection()) return;
            if (!ValidateQuantity()) return;

            var selectedProduct = (ProductListItem)ProductComboBox.SelectedItem;
            var quantity = int.Parse(QuantityTextBox.Text);
            var unitPrice = GetUnitPrice(selectedProduct);

            AddOrUpdateInvoiceItem(selectedProduct, quantity, unitPrice);
            ClearProductEntry();
            RefreshInvoiceGrid();
            UpdateTotals();
        }

        private bool ValidateProductSelection()
        {
            if (ProductComboBox.SelectedItem is not ProductListItem selectedProduct)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateQuantity()
        {
            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng hợp lệ.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return false;
            }
            
            // Check stock quantity
            if (ProductComboBox.SelectedItem is ProductListItem selectedProduct)
            {
                int currentStock = DatabaseHelper.GetProductStockQuantity(selectedProduct.Id);
                
                // Check if item already exists in invoice
                var existingItem = _invoiceItems.FirstOrDefault(i => i.ProductId == selectedProduct.Id);
                int requestedQuantity = quantity;
                if (existingItem != null)
                {
                    requestedQuantity += existingItem.Quantity;
                }
                
                if (requestedQuantity > currentStock)
                {
                    MessageBox.Show($"Không đủ hàng! Sản phẩm '{selectedProduct.Name}' chỉ còn {currentStock} sản phẩm trong kho.\nBạn đã có {existingItem?.Quantity ?? 0} sản phẩm trong hóa đơn.", 
                                  "Hết hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityTextBox.Focus();
                    return false;
                }
            }
            
            return true;
        }

        private decimal GetUnitPrice(ProductListItem product)
        {
            if (!string.IsNullOrWhiteSpace(UnitPriceTextBox.Text) &&
                decimal.TryParse(UnitPriceTextBox.Text, out decimal manualPrice) &&
                manualPrice >= 0)
            {
                return manualPrice;
            }
            return product.UnitPrice;
        }

        private void AddOrUpdateInvoiceItem(ProductListItem product, int quantity, decimal unitPrice)
        {
            var existingItem = _invoiceItems.FirstOrDefault(i =>
                i.ProductId == product.Id && i.UnitPrice == unitPrice);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.LineTotal = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                _invoiceItems.Add(new InvoiceItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    LineTotal = unitPrice * quantity
                });
            }
        }

        private void ClearProductEntry()
        {
            QuantityTextBox.Text = "1";
            UnitPriceTextBox.Text = string.Empty;
        }

        private void RefreshInvoiceGrid()
        {
            for (int i = 0; i < _invoiceItems.Count; i++)
            {
                _invoiceItems[i].RowNumber = i + 1;
            }

            InvoiceItemsDataGrid.ItemsSource = null;
            InvoiceItemsDataGrid.ItemsSource = _invoiceItems.ToList();
            
            // Update item count
            if (ItemCountTextBlock != null)
            {
                ItemCountTextBlock.Text = $"{_invoiceItems.Count} mục";
            }
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is InvoiceItemViewModel item)
            {
                _invoiceItems.Remove(item);
                RefreshInvoiceGrid();
                UpdateTotals();
            }
        }

        private void IncreaseQtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is InvoiceItemViewModel item)
            {
                // Check stock before increasing quantity
                int currentStock = DatabaseHelper.GetProductStockQuantity(item.ProductId);
                int requestedQuantity = item.Quantity + 1;
                
                if (requestedQuantity > currentStock)
                {
                    MessageBox.Show($"Không đủ hàng! Sản phẩm '{item.ProductName}' chỉ còn {currentStock} sản phẩm trong kho.\nHiện tại trong hóa đơn: {item.Quantity} sản phẩm.", 
                                  "Hết hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                item.Quantity++;
                item.LineTotal = item.UnitPrice * item.Quantity;
                RefreshInvoiceGrid();
                UpdateTotals();
            }
        }

        private void DecreaseQtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is InvoiceItemViewModel item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    item.LineTotal = item.UnitPrice * item.Quantity;
                }
                else
                {
                    _invoiceItems.Remove(item);
                }
                RefreshInvoiceGrid();
                UpdateTotals();
            }
        }

        private void ClearInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            ClearInvoice();
            UpdateTotals();
        }

        private void ClearInvoice()
        {
            _invoiceItems.Clear();
            RefreshInvoiceGrid();
        }

        #endregion

        #region Totals Calculation

        private void UpdateTotals()
        {
            if (!AreTotalsControlsReady()) return;

            try
            {
                var subtotal = _invoiceItems.Sum(item => item.LineTotal);
                UpdateSubtotalDisplay(subtotal);

                var taxPercent = GetTaxPercent();
                var discount = CalculateDiscount(subtotal);
                var tierDiscount = CalculateTierDiscount(subtotal);

                var taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
                var totalDiscount = discount + tierDiscount;
                var total = Math.Max(0, subtotal + taxAmount - totalDiscount);

                UpdateTotalsDisplay(taxAmount, totalDiscount, total);
                UpdateQRCode(total);
            }
            catch
            {
                // Silent failure
            }
        }

        private void UpdateSubtotalDisplay(decimal subtotal)
        {
            SubtotalTextBlock.Text = subtotal.ToString("F2");

            if (TierDiscountInlineText != null)
            {
                var tierDiscount = CalculateTierDiscount(subtotal);
                TierDiscountInlineText.Text = $"(+ Ưu đãi hạng: {tierDiscount:F2})";
            }
        }

        private decimal GetTaxPercent()
        {
            return decimal.TryParse(TaxPercentTextBox?.Text, out var tax) ? tax : 0m;
        }

        private decimal CalculateDiscount(decimal subtotal)
        {
            if (DiscountModeComboBox == null || DiscountValueTextBox == null) return 0m;

            var mode = (DiscountModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "VND";
            var discountValue = decimal.TryParse(DiscountValueTextBox.Text, out var value) ? value : 0m;

            return mode == "%" ? Math.Round(subtotal * (discountValue / 100m), 2) : discountValue;
        }

        private decimal CalculateTierDiscount(decimal subtotal)
        {
            var (tier, _) = GetSelectedCustomerLoyalty();
            var tierDiscountPercent = TierSettingsManager.GetTierDiscount(tier);
            return Math.Round(subtotal * (tierDiscountPercent / 100m), 2);
        }

        private void UpdateTotalsDisplay(decimal taxAmount, decimal discount, decimal total)
        {
            TaxAmountTextBlock.Text = taxAmount.ToString("F2");
            TotalTextBlock.Text = total.ToString("F2");

            var paid = decimal.TryParse(PaidTextBox?.Text, out var p) ? p : 0m;
            var change = Math.Max(0, paid - total);
            ChangeTextBlock.Text = change.ToString("F2");
        }

        private bool AreTotalsControlsReady()
        {
            return SubtotalTextBlock != null &&
                   TaxPercentTextBox != null &&
                   DiscountModeComboBox != null &&
                   DiscountValueTextBox != null &&
                   TaxAmountTextBlock != null &&
                   TotalTextBlock != null &&
                   PaidTextBox != null &&
                   ChangeTextBlock != null;
        }

        #endregion

        #region QR Code Management

        private void UpdateQRCode(decimal total)
        {
            try
            {
                var paymentSettings = PaymentSettingsManager.Load();

                if (InvoiceQRCode == null || PaymentMethodComboBox == null) return;

                var selectedPayment = GetSelectedPaymentMethod();

                if (!IsBankTransferSelected(selectedPayment))
                {
                    HideQRCode();
                    return;
                }

                if (!paymentSettings.EnableQRCode)
                {
                    HideQRCode();
                    return;
                }

                if (total <= 0)
                {
                    ShowErrorQRCode("Vui lòng thêm sản phẩm để tạo QR thanh toán");
                    return;
                }

                if (paymentSettings.BankAccount == null || paymentSettings.BankCode == null)
                {
                    ShowErrorQRCode("Chưa cấu hình thông tin ngân hàng. Vui lòng vào Settings để thiết lập");
                    return;
                }

                GenerateQRCode(paymentSettings, total);
            }
            catch
            {
                ShowErrorQRCode("Lỗi tạo QR code");
            }
        }

        private string GetSelectedPaymentMethod()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString() ?? "";
            }
            return "";
        }

        private bool IsBankTransferSelected(string paymentMethod)
        {
            return paymentMethod.Contains("Chuyển khoản");
        }

        private void GenerateQRCode(PaymentSettings settings, decimal total)
        {
            var description = GenerateTransactionDescription();
            var qrCode = QRCodeHelper.GenerateVietQRCode_Safe(
                settings.BankCode.ToLower(),
                settings.BankAccount,
                total,
                description,
                true,
                370,
                settings.AccountHolder);

            InvoiceQRCode.Source = qrCode;
            
            // Hide placeholder text when QR code is shown
            var placeholderText = this.FindName("QRPlaceholderText") as TextBlock;
            if (placeholderText != null)
                placeholderText.Visibility = Visibility.Collapsed;

            ShowQRCode();
        }

        private string GenerateTransactionDescription()
        {

            var description = "INV" + DateTime.Now.ToString("yyMMdd");
            description = Regex.Replace(description, @"[^a-zA-Z0-9]", "");

            if (description.Length > 8)
                description = description.Substring(0, 8);

            if (!description.StartsWith("INV"))
            {
                description = "INV" + DateTime.Now.ToString("yyMMdd");
                description = Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                if (description.Length > 8) description = description.Substring(0, 8);
            }

            return description;
        }

        private void ShowErrorQRCode(string message)
        {
            InvoiceQRCode.Source = CreateErrorQRCode(message, 120);
            
            // Show placeholder text for errors
            var placeholderText = this.FindName("QRPlaceholderText") as TextBlock;
            if (placeholderText != null)
            {
                placeholderText.Text = message;
                placeholderText.Visibility = Visibility.Visible;
            }

            ShowQRCode();
        }

        private void ShowQRCode()
        {
            if (InvoiceQRCode != null)
                InvoiceQRCode.Visibility = Visibility.Visible;
        }

        private void HideQRCode()
        {
            if (InvoiceQRCode != null)
            {
                InvoiceQRCode.Source = null;
                InvoiceQRCode.Visibility = Visibility.Collapsed;
            }

            // Show placeholder text when hiding
            var placeholderText = this.FindName("QRPlaceholderText") as TextBlock;
            if (placeholderText != null)
            {
                placeholderText.Text = "Chọn 'Chuyển khoản' để hiển thị QR";
                placeholderText.Visibility = Visibility.Visible;
            }
        }

        private BitmapSource CreateErrorQRCode(string message, int size)
        {
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // White background with red border
                drawingContext.DrawRectangle(
                    System.Windows.Media.Brushes.White,
                    new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2),
                    new System.Windows.Rect(0, 0, size, size));

                // Error message
                var formattedText = new System.Windows.Media.FormattedText(
                    message,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Arial"),
                    Math.Min(10, size / 12.0),
                    System.Windows.Media.Brushes.Red,
                    System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // Center the text
                double x = (size - formattedText.Width) / 2;
                double y = (size - formattedText.Height) / 2;

                drawingContext.DrawText(formattedText, new System.Windows.Point(x, y));
            }

            var renderTargetBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                size, size, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            return renderTargetBitmap;
        }

        #endregion

        #region Event Handlers

        private void TotalsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotals();
        }

        private void TotalsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTotals();
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var total = _invoiceItems.Sum(item => item.LineTotal);
            UpdateQRCode(total);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    AddItemButton_Click(AddItemButton, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Delete:
                    if (InvoiceItemsDataGrid?.SelectedItem is InvoiceItemViewModel selectedItem)
                    {
                        var button = new Button { DataContext = selectedItem };
                        RemoveItemButton_Click(button, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;

                case System.Windows.Input.Key.S when e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control:
                    SaveInvoiceButton_Click(SaveInvoiceButton, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Invoice Operations

        private void SaveInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInvoiceForSave()) return;

            var customerId = (int)CustomerComboBox.SelectedValue;
            var paid = decimal.TryParse(PaidTextBox?.Text, out var p) ? p : 0m;

            if (CreateAndSaveInvoice(customerId, paid))
            {
                ProcessSuccessfulSave(customerId);
            }
        }

        private bool ValidateInvoiceForSave()
        {
            if (_invoiceItems.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một sản phẩm.", "Xác thực",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CustomerComboBox.SelectedValue is not int customerId || customerId <= 0)
            {
                MessageBox.Show("Vui lòng chọn khách hàng.", "Xác thực",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool CreateAndSaveInvoice(int customerId, decimal paid)
        {
            try
            {
                var subtotal = _invoiceItems.Sum(item => item.LineTotal);
                var taxPercent = GetTaxPercent();
                var discount = CalculateTotalDiscount(subtotal);
                var taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
                var total = Math.Max(0, subtotal + taxAmount - discount);

                var itemsForSave = _invoiceItems.Select(item =>
                    (item.ProductId, item.Quantity, item.UnitPrice)).ToList();

                var currentUser = Application.Current.Resources["CurrentUser"]?.ToString() ?? "admin";
                var employeeId = DatabaseHelper.GetEmployeeIdByUsername(currentUser);

                System.Diagnostics.Debug.WriteLine($"Attempting to save invoice: CustomerId={customerId}, EmployeeId={employeeId}, Items={itemsForSave.Count}");

                var result = DatabaseHelper.SaveInvoice(customerId, employeeId, subtotal, taxPercent,
                    taxAmount, discount, total, paid, itemsForSave);

                if (!result)
                {
                    MessageBox.Show($"Không thể lưu hóa đơn. Vui lòng kiểm tra:\n" +
                                   $"1. Database connection\n" +
                                   $"2. Customer ID: {customerId}\n" +
                                   $"3. Employee ID: {employeeId}\n" +
                                   $"4. Products có tồn tại trong database\n" +
                                   $"Xem Debug Output để biết chi tiết.",
                                   "Lỗi",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu hóa đơn: {ex.Message}\n\nChi tiết: {ex.StackTrace}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"SaveInvoice exception: {ex}");
                return false;
            }
        }

        private decimal CalculateTotalDiscount(decimal subtotal)
        {
            var manualDiscount = CalculateDiscount(subtotal);
            var tierDiscount = CalculateTierDiscount(subtotal);
            return manualDiscount + tierDiscount;
        }

        private void ProcessSuccessfulSave(int customerId)
        {
            var invoiceId = DatabaseHelper.LastSavedInvoiceId;
            MessageBox.Show($"Hóa đơn #{invoiceId} đã được lưu.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateCustomerLoyalty(customerId);
            ClearInvoiceAfterSave();
            ShowPrintWindow(invoiceId);

            DashboardWindow.TriggerDashboardRefresh();
        }

        private void ShowPrintWindow(int invoiceId)
        {
            try
            {
                var currentUser = Application.Current.Resources["CurrentUser"]?.ToString() ?? "admin";
                var employeeId = DatabaseHelper.GetEmployeeIdByUsername(currentUser);

                var printWindow = new InvoicePrintWindow(invoiceId, employeeId);
                printWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hiển thị cửa sổ in: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCustomerLoyalty(int customerId)
        {
            try
            {
                var subtotal = _invoiceItems.Sum(item => item.LineTotal);
                var discount = CalculateTotalDiscount(subtotal);
                var total = Math.Max(0, subtotal - discount);

                var (_, currentPoints) = DatabaseHelper.GetCustomerLoyalty(customerId);
                var earnedPoints = (int)Math.Floor((double)total / 100000);
                var newPoints = currentPoints + earnedPoints;
                var newTier = TierSettingsManager.DetermineTierByPoints(newPoints);

                DatabaseHelper.UpdateCustomerLoyalty(customerId, newPoints, newTier);
            }
            catch
            {
                // Silent failure
            }
        }

        private void ClearInvoiceAfterSave()
        {
            _invoiceItems.Clear();
            RefreshInvoiceGrid();
            ResetFormFields();
            UpdateTotals();
        }

        private void ResetFormFields()
        {
            TaxPercentTextBox.Text = "0";
            if (DiscountValueTextBox != null) DiscountValueTextBox.Text = "0";
            PaidTextBox.Text = "0";
        }

        private void OpenHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new TransactionHistoryWindow();
            historyWindow.ShowDialog();
        }

        #endregion

        #region Helper Methods

        private static string GetTextOrEmpty(TextBox textBox)
        {
            return textBox?.Text ?? string.Empty;
        }

        private static decimal TryGetDecimal(string text)
        {
            return decimal.TryParse(text, out var value) && value >= 0 ? value : 0m;
        }

        private int GetEmployeeId(string username)
        {
            try
            {
                return DatabaseHelper.GetEmployeeIdByUsername(username);
            }
            catch
            {
                return 1; // Default to admin
            }
        }

        #endregion
    }

    #region View Models

    public class InvoiceItemViewModel
    {
        public int RowNumber { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class ProductListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
    }

    public class CustomerListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    #endregion
}
