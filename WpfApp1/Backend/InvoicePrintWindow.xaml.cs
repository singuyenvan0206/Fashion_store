using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class InvoicePrintWindow : Window
    {
        private readonly List<InvoiceItemViewModel> _items;
        private readonly CustomerListItem? _customer;
        private readonly decimal _subtotal;
        private readonly decimal _taxPercent;
        private readonly decimal _taxAmount;
        private readonly decimal _discount;
        private readonly decimal _total;
        private readonly int _invoiceId;
        private readonly DateTime _invoiceDate;

        public InvoicePrintWindow(List<InvoiceItemViewModel> items, CustomerListItem customer, 
            decimal subtotal, decimal taxPercent, decimal taxAmount, decimal discount, 
            decimal total, int invoiceId, DateTime invoiceDate)
        {
            InitializeComponent();
            
            _items = items;
            _customer = customer;
            _subtotal = subtotal;
            _taxPercent = taxPercent;
            _taxAmount = taxAmount;
            _discount = discount;
            _total = total;
            _invoiceId = invoiceId;
            _invoiceDate = invoiceDate;

            LoadInvoiceData();
        }


        // Load from database by saved invoice id
        public InvoicePrintWindow(int invoiceId, int employeeId)
        {
            InitializeComponent();

            _items = new List<InvoiceItemViewModel>();
            _customer = new CustomerListItem();
            _subtotal = 0m;
            _taxPercent = 0m;
            _taxAmount = 0m;
            _discount = 0m;
            _total = 0m;
            _invoiceId = invoiceId;
            _invoiceDate = DateTime.Now;

            LoadFromDatabase(invoiceId, employeeId);
        }

        private void LoadInvoiceData()
        {
            // Set invoice information
            InvoiceDateText.Text = _invoiceDate.ToString("dd/MM/yyyy");
            InvoiceNumberText.Text = _invoiceId.ToString();
            InvoiceForText.Text = "Giao dịch bán hàng";

            // Set customer information
            CustomerNameText.Text = string.IsNullOrWhiteSpace(_customer?.Name) ? "Khách lẻ" : _customer.Name;
            CustomerPhoneText.Text = string.IsNullOrWhiteSpace(_customer?.Phone) ? "Không có" : _customer.Phone;
            CustomerEmailText.Text = "Không có";
            CustomerAddressText.Text = "Không có";

            // Load items
            InvoiceItemsList.ItemsSource = _items;

            // Set totals
            SubtotalText.Text = _subtotal.ToString("C");
            TaxRateText.Text = _taxPercent.ToString("F2") + "%";
            SalesTaxText.Text = _taxAmount.ToString("C");
            OtherText.Text = _discount.ToString("C");
            TotalText.Text = _total.ToString("C");

            // Generate QR code for payment
            GeneratePaymentQRCode(_invoiceId, _total);
        }

        private void LoadFromDatabase(int invoiceId, int employeeId)
        {
            try
            {
                var (header, items) = DatabaseHelper.GetInvoiceDetails(invoiceId);

                // Header
                InvoiceDateText.Text = header.CreatedDate.ToString("dd/MM/yyyy");
                InvoiceTimeText.Text = header.CreatedDate.ToString("HH:mm");
                InvoiceNumberText.Text = header.Id.ToString();
                InvoiceForText.Text = "Giao dịch bán hàng";

                // Employee display
                try
                {
                    var accounts = DatabaseHelper.GetAllAccounts();
                    var employee = accounts.FirstOrDefault(a => a.Id == employeeId);
                    EmployeeNameText.Text = employee != default 
                        ? (string.IsNullOrWhiteSpace(employee.EmployeeName) ? employee.Username : employee.EmployeeName)
                        : "Không xác định";
                }
                catch
                {
                    EmployeeNameText.Text = "Không xác định";
                }

                // Customer
                CustomerNameText.Text = string.IsNullOrWhiteSpace(header.CustomerName) ? "Khách lẻ" : header.CustomerName;
                CustomerPhoneText.Text = string.IsNullOrWhiteSpace(header.CustomerPhone) ? "Không có" : header.CustomerPhone;
                CustomerEmailText.Text = string.IsNullOrWhiteSpace(header.CustomerEmail) ? "Không có" : header.CustomerEmail;
                CustomerAddressText.Text = string.IsNullOrWhiteSpace(header.CustomerAddress) ? "Không có" : header.CustomerAddress;

                // Items
                var vmItems = new List<InvoiceItemViewModel>();
                int row = 1;
                foreach (var it in items)
                {
                    vmItems.Add(new InvoiceItemViewModel
                    {
                        RowNumber = row++,
                        ProductId = it.ProductId,
                        ProductName = it.ProductName,
                        UnitPrice = it.UnitPrice,
                        Quantity = it.Quantity,
                        LineTotal = it.LineTotal
                    });
                }
                InvoiceItemsList.ItemsSource = vmItems;

                // Totals
                SubtotalText.Text = header.Subtotal.ToString("C");
                var taxPercent = header.Subtotal == 0 ? 0m : (header.TaxAmount / header.Subtotal * 100m);
                TaxRateText.Text = taxPercent.ToString("F2") + "%";
                SalesTaxText.Text = header.TaxAmount.ToString("C");
                OtherText.Text = header.Discount.ToString("C");
                TotalText.Text = header.Total.ToString("C");
                PaidText.Text = header.Paid.ToString("C");

                // Generate QR code for payment
                GeneratePaymentQRCode(invoiceId, header.Total);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được dữ liệu hóa đơn #{invoiceId}: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();

                // Configure print settings safely (PrintTicket can be null on some drivers)
                var ticket = printDialog.PrintTicket ?? new PrintTicket();
                ticket.PageOrientation = PageOrientation.Portrait;
                ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
                printDialog.PrintTicket = ticket;

                if (printDialog.ShowDialog() == true)
                {
                    var toPrint = CreatePrintVisual();

                    printDialog.PrintVisual(toPrint, "Invoice #" + _invoiceId);
                    
                    MessageBox.Show("Invoice printed successfully!", "Print Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Print Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreatePrintVisual()
        {
            var printGrid = new Grid { Width = 800, Background = Brushes.White };
            printGrid.Children.Add(CreateInvoiceContent());
            return printGrid;
        }

        private FrameworkElement CreateInvoiceContent()
        {
            var mainGrid = new Grid();
            mainGrid.Width = 800;
            mainGrid.Margin = new Thickness(50);

            // Define rows
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header Section
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Company Info
            var companyStack = new StackPanel();
            companyStack.Children.Add(new TextBlock { Text = "HỆ THỐNG QUẢN LÝ BÁN HÀNG", FontSize = 24, FontWeight = FontWeights.Bold });

            Grid.SetColumn(companyStack, 0);
            headerGrid.Children.Add(companyStack);

            // Invoice Title
            var invoiceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            invoiceStack.Children.Add(new TextBlock { Text = "HÓA ĐƠN", FontSize = 28, FontWeight = FontWeights.Bold });
            invoiceStack.Children.Add(CreateInfoRow("Ngày:", _invoiceDate.ToString("dd/MM/yyyy")));
            invoiceStack.Children.Add(CreateInfoRow("Số HĐ:", _invoiceId.ToString()));
            Grid.SetColumn(invoiceStack, 1);
            headerGrid.Children.Add(invoiceStack);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Customer Info
            var customerStack = new StackPanel();
            customerStack.Children.Add(new TextBlock { Text = "KHÁCH HÀNG:", FontWeight = FontWeights.Bold });
            customerStack.Children.Add(new TextBlock { Text = _customer?.Name ?? "Khách lẻ" });
            if (!string.IsNullOrWhiteSpace(_customer?.Phone))
                customerStack.Children.Add(new TextBlock { Text = _customer.Phone });
            Grid.SetRow(customerStack, 1);
            mainGrid.Children.Add(customerStack);

            // Items Table
            var itemsTable = CreateItemsTable();
            Grid.SetRow(itemsTable, 2);
            mainGrid.Children.Add(itemsTable);

            // Totals Section
            var totalsGrid = CreateTotalsSection();
            Grid.SetRow(totalsGrid, 3);
            mainGrid.Children.Add(totalsGrid);

            // Thank You Message
            var thankYouText = new TextBlock
            {
                Text = "THANK YOU FOR YOUR BUSINESS!",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 136, 229)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(thankYouText, 4);
            mainGrid.Children.Add(thankYouText);

            return mainGrid;
        }

        private StackPanel CreateInfoRow(string label, string value)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, Width = 60 });
            stack.Children.Add(new TextBlock { Text = value });
            return stack;
        }

        private FrameworkElement CreateItemsTable()
        {
            var table = new StackPanel();
            
            // Header
            var header = new Grid { Background = Brushes.LightGray };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            header.Children.Add(new TextBlock { Text = "Sản phẩm", FontWeight = FontWeights.Bold, Margin = new Thickness(5) });
            var qty = new TextBlock { Text = "SL", FontWeight = FontWeights.Bold, Margin = new Thickness(5) };
            Grid.SetColumn(qty, 1);
            header.Children.Add(qty);
            var price = new TextBlock { Text = "Đơn giá", FontWeight = FontWeights.Bold, Margin = new Thickness(5) };
            Grid.SetColumn(price, 2);
            header.Children.Add(price);
            var total = new TextBlock { Text = "Thành tiền", FontWeight = FontWeights.Bold, Margin = new Thickness(5) };
            Grid.SetColumn(total, 3);
            header.Children.Add(total);
            
            table.Children.Add(header);
            
            // Items
            foreach (var item in _items)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                row.Children.Add(new TextBlock { Text = item.ProductName, Margin = new Thickness(5) });
                var itemQty = new TextBlock { Text = item.Quantity.ToString(), Margin = new Thickness(5) };
                Grid.SetColumn(itemQty, 1);
                row.Children.Add(itemQty);
                var itemPrice = new TextBlock { Text = item.UnitPrice.ToString("C"), Margin = new Thickness(5) };
                Grid.SetColumn(itemPrice, 2);
                row.Children.Add(itemPrice);
                var itemTotal = new TextBlock { Text = item.LineTotal.ToString("C"), Margin = new Thickness(5) };
                Grid.SetColumn(itemTotal, 3);
                row.Children.Add(itemTotal);
                
                table.Children.Add(row);
            }
            
            return table;
        }

        private Grid CreateTotalsSection()
        {
            var totalsGrid = new Grid();
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Left side - Notes
            var notesStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            notesStack.Children.Add(new TextBlock { Text = "Cảm ơn quý khách đã sử dụng dịch vụ!", FontSize = 12 });
            Grid.SetColumn(notesStack, 0);
            totalsGrid.Children.Add(notesStack);

            // Right side - Totals
            var totalsStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            var totalsTable = new Grid();
            totalsTable.Width = 280;

            // Simple totals
            totalsTable.Children.Add(new TextBlock { Text = $"Tạm tính: {_subtotal:C}", HorizontalAlignment = HorizontalAlignment.Right });
            totalsTable.Children.Add(new TextBlock { Text = $"Thuế: {_taxAmount:C}", HorizontalAlignment = HorizontalAlignment.Right });
            totalsTable.Children.Add(new TextBlock { Text = $"Giảm giá: {_discount:C}", HorizontalAlignment = HorizontalAlignment.Right });
            totalsTable.Children.Add(new TextBlock { Text = $"Tổng cộng: {_total:C}", FontWeight = FontWeights.Bold, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Right });

            totalsStack.Children.Add(totalsTable);
            Grid.SetColumn(totalsStack, 1);
            totalsGrid.Children.Add(totalsStack);

            return totalsGrid;
        }


        private void GeneratePaymentQRCode(int invoiceId, decimal total)
        {
            try
            {
                // Load payment settings
                var paymentSettings = PaymentSettingsManager.Load();
                
                if (!paymentSettings.EnableQRCode)
                {
                    // Ẩn QR code nếu bị tắt
                    if (PaymentQRCode?.Parent is Border qrBorder)
                    {
                        qrBorder.Visibility = Visibility.Collapsed;
                    }
                    return;
                }

                // Lấy thông tin thanh toán từ settings
                string paymentMethod = paymentSettings.PaymentMethod.ToLower();
                string paymentInfo = paymentMethod switch
                {
                    "momo" => paymentSettings.MoMoPhone,
                    "zalopay" => paymentSettings.ZaloPayPhone,
                    "bank" => paymentSettings.BankAccount,
                    _ => paymentSettings.MoMoPhone
                };
                
                // Tạo QR code
                var qrCode = QRCodeHelper.GenerateInvoicePaymentQR(invoiceId, total, paymentMethod, paymentInfo);
                
                // Hiển thị QR code
                if (PaymentQRCode != null)
                {
                    PaymentQRCode.Source = qrCode;
                }
            }
            catch
            {

            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
