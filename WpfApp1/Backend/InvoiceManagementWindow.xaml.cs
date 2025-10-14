using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class InvoiceManagementWindow : Window
    {
        private readonly List<InvoiceItemViewModel> _items = new();

        public InvoiceManagementWindow()
        {
            InitializeComponent();
            LoadCustomers();
            LoadProducts();
            RefreshItemsGrid();
            RecalculateTotals();
            InitializeQRCodeState();
        }

        private void InitializeQRCodeState()
        {
            try
            {
                var paymentSettings = PaymentSettingsManager.Load();
                if (InvoiceQRCode != null)
                {
                    if (!paymentSettings.EnableQRCode)
                    {
                        InvoiceQRCode.Source = null;
                        InvoiceQRCode.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        InvoiceQRCode.Visibility = Visibility.Visible;
                        LoadStaticQRCode(paymentSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing QR code state: {ex.Message}");
            }
        }

        private void LoadStaticQRCode(PaymentSettings paymentSettings)
        {
            try
            {
                // Sử dụng QR code chuyển khoản cố định theo phương thức thanh toán
                InvoiceQRCode.Source = QRCodeHelper.GenerateQRByMethod(paymentSettings.PaymentMethod);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading static QR code: {ex.Message}");
                // Fallback: sử dụng TPBank QR mặc định
                InvoiceQRCode.Source = QRCodeHelper.GenerateTPBankQR();
            }
        }

        // Đã chuyển sang sử dụng QRCodeHelper.GenerateQRByMethod() thay vì các method riêng lẻ

        private void LoadCustomers()
        {
            var customers = DatabaseHelper.GetAllCustomers();
            var customerVms = customers.ConvertAll(c => new CustomerListItem { Id = c.Id, Name = c.Name, Phone = c.Phone });
            CustomerComboBox.ItemsSource = customerVms;
            if (customerVms.Count > 0)
            {
                CustomerComboBox.SelectedIndex = 0;
            }
            UpdateLoyaltyHeader();
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLoyaltyHeader();
            RecalculateTotals();
        }

        private void UpdateLoyaltyHeader()
        {
            var (tier, points) = GetSelectedCustomerLoyalty();
            if (LoyaltyTierTextBlock != null) LoyaltyTierTextBlock.Text = tier;
            if (LoyaltyPointsTextBlock != null) LoyaltyPointsTextBlock.Text = $"{points} điểm";
        }

        private (string tier, int points) GetSelectedCustomerLoyalty()
        {
            if (CustomerComboBox?.SelectedValue is int cid && cid > 0)
            {
                return DatabaseHelper.GetCustomerLoyalty(cid);
            }
            return ("Regular", 0);
        }

        private void LoadProducts()
        {
            var products = DatabaseHelper.GetAllProducts();
            var productVms = products.ConvertAll(p => new ProductListItem
            {
                Id = p.Id,
                Name = string.IsNullOrWhiteSpace(p.Code) ? p.Name : $"{p.Name} ({p.Code})",
                UnitPrice = p.SalePrice
            });
            ProductComboBox.ItemsSource = productVms;
        }

        private void RefreshItemsGrid()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].RowNumber = i + 1;
            }
            InvoiceItemsDataGrid.ItemsSource = null;
            InvoiceItemsDataGrid.ItemsSource = _items.ToList();
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is not ProductListItem selectedProduct)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng hợp lệ.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            decimal unitPrice = selectedProduct.UnitPrice;
            if (!string.IsNullOrWhiteSpace(UnitPriceTextBox.Text) && decimal.TryParse(UnitPriceTextBox.Text, out decimal manualPrice) && manualPrice >= 0)
            {
                unitPrice = manualPrice;
            }

            var existing = _items.FirstOrDefault(i => i.ProductId == selectedProduct.Id && i.UnitPrice == unitPrice);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.LineTotal = existing.UnitPrice * existing.Quantity;
            }
            else
            {
                _items.Add(new InvoiceItemViewModel
                {
                    ProductId = selectedProduct.Id,
                    ProductName = selectedProduct.Name,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    LineTotal = unitPrice * quantity
                });
            }

            QuantityTextBox.Text = "1";
            UnitPriceTextBox.Text = string.Empty;
            RefreshItemsGrid();
            RecalculateTotals();
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductListItem selected)
            {
                UnitPriceTextBox.Text = selected.UnitPrice.ToString("F2");
                QuantityTextBox.Text = string.IsNullOrWhiteSpace(QuantityTextBox.Text) ? "1" : QuantityTextBox.Text;
            }
            RecalculateTotals();
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItemViewModel item)
            {
                _items.Remove(item);
                RefreshItemsGrid();
                RecalculateTotals();
            }
        }

        private void IncreaseQtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItemViewModel item)
            {
                item.Quantity += 1;
                item.LineTotal = item.UnitPrice * item.Quantity;
                RefreshItemsGrid();
                RecalculateTotals();
            }
        }

        private void DecreaseQtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItemViewModel item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                    item.LineTotal = item.UnitPrice * item.Quantity;
                }
                else
                {
                    _items.Remove(item);
                }
                RefreshItemsGrid();
                RecalculateTotals();
            }
        }

        private void ClearInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
            RefreshItemsGrid();
            RecalculateTotals();
        }

        private void UpdateInvoiceQRCode(decimal total)
        {
            try
            {
                var paymentSettings = PaymentSettingsManager.Load();
                
                if (InvoiceQRCode == null || PaymentMethodComboBox == null)
                {
                    return;
                }

                // Lấy phương thức thanh toán từ ComboBox
                string selectedPayment = "";
                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    selectedPayment = selectedItem.Content?.ToString() ?? "";
                }

                // Chỉ hiển thị QR code cho chuyển khoản và ví điện tử
                if (!selectedPayment.Contains("Chuyển khoản") && !selectedPayment.Contains("Ví điện tử"))
                {
                    InvoiceQRCode.Source = null;
                    InvoiceQRCode.Visibility = Visibility.Collapsed;
                    return;
                }

                if (!paymentSettings.EnableQRCode)
                {
                    InvoiceQRCode.Source = null;
                    InvoiceQRCode.Visibility = Visibility.Collapsed;
                    return;
                }

                InvoiceQRCode.Visibility = Visibility.Visible;
                
                // Quyết định loại QR code dựa trên lựa chọn
                string qrMethod = paymentSettings.PaymentMethod; // Mặc định từ settings
                if (selectedPayment.Contains("Ví điện tử"))
                {
                    qrMethod = "momo"; // Sử dụng MoMo cho ví điện tử
                }
                
                var qrCode = QRCodeHelper.GenerateQRByMethod(qrMethod);
                InvoiceQRCode.Source = qrCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating QR code: {ex.Message}");
            }
        }


        private void TotalsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            if (!TotalsControlsReady())
            {
                return;
            }

            decimal subtotal = _items.Sum(i => i.LineTotal);
            SubtotalTextBlock.Text = subtotal.ToString("F2");

            decimal taxPercent = TryGetDecimal(GetTextOrEmpty(TaxPercentTextBox));
            decimal discount = 0m;
            // New unified discount controls
            if (DiscountModeComboBox != null && DiscountValueTextBox != null)
            {
                var modeText = (DiscountModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "VND";
                var discountVal = TryGetDecimal(GetTextOrEmpty(DiscountValueTextBox));
                if (string.Equals(modeText, "%", StringComparison.Ordinal))
                {
                    discount = Math.Round(subtotal * (discountVal / 100m), 2);
                }
                else
                {
                    discount = discountVal;
                }
            }

            // Loyalty tier auto-discount
            var (tier, _) = GetSelectedCustomerLoyalty();
            decimal tierDiscountPercent = TierSettingsManager.GetTierDiscount(tier);
            decimal tierDiscount = Math.Round(subtotal * (tierDiscountPercent / 100m), 2);
            if (TierDiscountInlineText != null) TierDiscountInlineText.Text = $"(+ Ưu đãi hạng: {tierDiscount:F2})";
            discount += tierDiscount;

            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            TaxAmountTextBlock.Text = taxAmount.ToString("F2");

            decimal total = Math.Max(0, subtotal + taxAmount - discount);
            TotalTextBlock.Text = total.ToString("F2");

            decimal paid = TryGetDecimal(GetTextOrEmpty(PaidTextBox));
            decimal change = Math.Max(0, paid - total);
            ChangeTextBlock.Text = change.ToString("F2");

            // Update QR code when totals change
            UpdateInvoiceQRCode(total);
        }

        private static string GetTextOrEmpty(TextBox? textBox)
        {
            return textBox?.Text ?? string.Empty;
        }

        private bool TotalsControlsReady()
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

        private void TotalsInput_Toggle(object sender, RoutedEventArgs e)
        {
            RecalculateTotals();
        }

        private void TotalsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateTotals();
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Cập nhật QR code khi thay đổi phương thức thanh toán
            decimal total = _items.Sum(i => i.LineTotal);
            UpdateInvoiceQRCode(total);
        }


        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // Focus product entry for quick keyboard usage
            ProductComboBox?.Focus();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddItemButton_Click(AddItemButton, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (e.Key == System.Windows.Input.Key.Delete && InvoiceItemsDataGrid?.SelectedItem is InvoiceItemViewModel)
            {
                RemoveItemButton_Click(RemoveItemButtonFromSelection(), new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control
                && e.Key == System.Windows.Input.Key.S)
            {
                SaveInvoiceButton_Click(SaveInvoiceButton, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private Button RemoveItemButtonFromSelection()
        {
            var btn = new Button();
            btn.DataContext = InvoiceItemsDataGrid?.SelectedItem;
            return btn;
        }

        private static decimal TryGetDecimal(string? text)
        {
            if (decimal.TryParse(text, out var value) && value >= 0)
                return value;
            return 0m;
        }

        private int GetEmployeeId(string username)
        {
            return DatabaseHelper.GetEmployeeIdByUsername(username);
        }

        private void PrintInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một sản phẩm để in hóa đơn.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CustomerComboBox.SelectedItem is not CustomerListItem customer)
            {
                MessageBox.Show("Vui lòng chọn khách hàng.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal subtotal = _items.Sum(i => i.LineTotal);
            decimal taxPercent = TryGetDecimal(TaxPercentTextBox.Text);
            decimal discount = 0m;
            if (DiscountModeComboBox != null && DiscountValueTextBox != null)
            {
                var modeText = (DiscountModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "VND";
                var discountVal = TryGetDecimal(DiscountValueTextBox.Text);
                if (string.Equals(modeText, "%", StringComparison.Ordinal))
                {
                    discount = Math.Round(subtotal * (discountVal / 100m), 2);
                }
                else
                {
                    discount = discountVal;
                }
            }
            var (stier, _) = GetSelectedCustomerLoyalty();
            decimal sTierPercent = TierSettingsManager.GetTierDiscount(stier);
            discount += Math.Round(subtotal * (sTierPercent / 100m), 2);
            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            decimal total = Math.Max(0, subtotal + taxAmount - discount);

            int tempInvoiceId = new Random().Next(1000, 9999);
            DateTime invoiceDate = DateTime.Now;
            string currentUser = Application.Current.Resources["CurrentUser"]?.ToString() ?? "admin";
            var employeeId = GetEmployeeId(currentUser);
            var itemsForSave = _items.Select(i => (i.ProductId, i.Quantity, i.UnitPrice)).ToList();
            var success = DatabaseHelper.SaveInvoice(customer.Id, employeeId, subtotal, taxPercent, taxAmount, discount, total, 0, itemsForSave);
            if (success)
            {

                var invoiceId = DatabaseHelper.LastSavedInvoiceId;
                var printWindow = new InvoicePrintWindow(invoiceId, employeeId);
                printWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Không thể lưu hóa đơn để in.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất một sản phẩm.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CustomerComboBox.SelectedValue is not int customerId || customerId <= 0)
            {
                MessageBox.Show("Vui lòng chọn khách hàng.", "Xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal subtotal = _items.Sum(i => i.LineTotal);
            decimal taxPercent = TryGetDecimal(TaxPercentTextBox.Text);
            decimal discount = 0m;
            if (DiscountModeComboBox != null && DiscountValueTextBox != null)
            {
                var modeText = (DiscountModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "VND";
                var discountVal = TryGetDecimal(DiscountValueTextBox.Text);
                if (string.Equals(modeText, "%", StringComparison.Ordinal))
                {
                    discount = Math.Round(subtotal * (discountVal / 100m), 2);
                }
                else
                {
                    discount = discountVal;
                }
            }
            var (saveTier, savePts) = GetSelectedCustomerLoyalty();
            decimal saveTierPercent = TierSettingsManager.GetTierDiscount(saveTier);
            discount += Math.Round(subtotal * (saveTierPercent / 100m), 2);
            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            decimal total = Math.Max(0, subtotal + taxAmount - discount);
            decimal paid = TryGetDecimal(PaidTextBox.Text);

            var itemsToSave = _items.Select(i => (i.ProductId, i.Quantity, i.UnitPrice)).ToList();
            
            // Lấy EmployeeId của người đang đăng nhập
            string currentUser = Application.Current.Resources["CurrentUser"]?.ToString() ?? "admin";
            var employeeId = GetEmployeeId(currentUser);
            
            // Debug: Log thông tin để kiểm tra
            System.Diagnostics.Debug.WriteLine($"Current user: {currentUser}");
            System.Diagnostics.Debug.WriteLine($"Employee ID: {employeeId}");
            
            bool ok = DatabaseHelper.SaveInvoice(customerId, employeeId, subtotal, taxPercent, taxAmount, discount, total, paid, itemsToSave);
            if (ok)
            {
                int invoiceId = DatabaseHelper.LastSavedInvoiceId;
                MessageBox.Show($"Hóa đơn #{invoiceId} đã được lưu.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                // Update loyalty points and tier
                int earned = (int)Math.Floor((double)total / 100000); // 1 point per 100k
                int newPoints = savePts + earned;
                string newTier = TierSettingsManager.DetermineTierByPoints(newPoints);
                DatabaseHelper.UpdateCustomerLoyalty(customerId, newPoints, newTier);

                _items.Clear();
                RefreshItemsGrid();
                TaxPercentTextBox.Text = "0";
                if (DiscountValueTextBox != null) DiscountValueTextBox.Text = "0";
                PaidTextBox.Text = "0";
                RecalculateTotals();

           
                try
                {
                    var selectedCustomer = CustomerComboBox.SelectedItem as CustomerListItem;
                    var printWindow = new InvoicePrintWindow(invoiceId, employeeId);
                    printWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở cửa sổ in cho hóa đơn #{invoiceId}: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Không thể lưu hóa đơn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new TransactionHistoryWindow();
            try
            {
                if (this.IsLoaded)
                {
                    historyWindow.Owner = this;
                    historyWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    historyWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            catch (InvalidOperationException)
            {
                historyWindow.Owner = null;
                historyWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            historyWindow.ShowDialog();
        }
    }

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
    }

    public class CustomerListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}

