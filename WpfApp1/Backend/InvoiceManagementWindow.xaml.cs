using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void LoadCustomers()
        {
            var customers = DatabaseHelper.GetAllCustomers();
            var customerVms = customers.ConvertAll(c => new CustomerListItem { Id = c.Id, Name = c.Name });
            CustomerComboBox.ItemsSource = customerVms;
            if (customerVms.Count > 0)
            {
                CustomerComboBox.SelectedIndex = 0;
            }
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
            decimal discount = TryGetDecimal(GetTextOrEmpty(DiscountTextBox));
            if (UsePercentDiscountCheckBox != null && UsePercentDiscountCheckBox.IsChecked == true)
            {
                decimal discPercent = TryGetDecimal(GetTextOrEmpty(DiscountPercentTextBox));
                discount = Math.Round(subtotal * (discPercent / 100m), 2);
            }

            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            TaxAmountTextBlock.Text = taxAmount.ToString("F2");

            decimal total = Math.Max(0, subtotal + taxAmount - discount);
            TotalTextBlock.Text = total.ToString("F2");

            decimal paid = TryGetDecimal(GetTextOrEmpty(PaidTextBox));
            decimal change = Math.Max(0, paid - total);
            ChangeTextBlock.Text = change.ToString("F2");
        }

        private static string GetTextOrEmpty(TextBox? textBox)
        {
            return textBox?.Text ?? string.Empty;
        }

        private bool TotalsControlsReady()
        {
            return SubtotalTextBlock != null &&
                   TaxPercentTextBox != null &&
                   DiscountTextBox != null &&
                   (DiscountPercentTextBox != null) &&
                   TaxAmountTextBlock != null &&
                   TotalTextBlock != null &&
                   PaidTextBox != null &&
                   ChangeTextBlock != null;
        }

        private void TotalsInput_Toggle(object sender, RoutedEventArgs e)
        {
            RecalculateTotals();
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
            decimal discount = TryGetDecimal(DiscountTextBox.Text);
            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            decimal total = Math.Max(0, subtotal + taxAmount - discount);

            // Generate a temporary invoice ID for preview
            int tempInvoiceId = new Random().Next(1000, 9999);
            DateTime invoiceDate = DateTime.Now;

            var printWindow = new InvoicePrintWindow(_items, customer, subtotal, taxPercent, taxAmount, discount, total, tempInvoiceId, invoiceDate);
            printWindow.ShowDialog();
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
            decimal discount = TryGetDecimal(DiscountTextBox.Text);
            decimal taxAmount = Math.Round(subtotal * (taxPercent / 100m), 2);
            decimal total = Math.Max(0, subtotal + taxAmount - discount);
            decimal paid = TryGetDecimal(PaidTextBox.Text);

            var itemsToSave = _items.Select(i => (i.ProductId, i.Quantity, i.UnitPrice)).ToList();
            
            // Lấy EmployeeId của người đang đăng nhập
            string currentUser = Application.Current.Resources["CurrentUser"]?.ToString() ?? "admin";
            var employeeId = GetEmployeeId(currentUser);
            
            bool ok = DatabaseHelper.SaveInvoice(customerId, employeeId, subtotal, taxPercent, taxAmount, discount, total, paid, itemsToSave);
            if (ok)
            {
                int invoiceId = DatabaseHelper.LastSavedInvoiceId;
                MessageBox.Show($"Hóa đơn #{invoiceId} đã được lưu.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
             
                _items.Clear();
                RefreshItemsGrid();
                TaxPercentTextBox.Text = "0";
                DiscountTextBox.Text = "0";
                PaidTextBox.Text = "0";
                RecalculateTotals();
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
    }
}
