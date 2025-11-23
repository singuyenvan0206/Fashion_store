
using System.Printing;
using System.Windows;
using System.Windows.Controls;

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
        private readonly int _employeeId;
        private DatabaseHelper.InvoiceHeader? _invoiceHeader;

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
            _employeeId = 1; // Giá trị mặc định cho constructor cũ

            LoadInvoiceData();
        }


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
            _employeeId = employeeId;
            _invoiceDate = DateTime.Now;

            LoadFromDatabase(invoiceId, employeeId);
        }

        private void SetText(string elementName, string text)
        {
            if (FindName(elementName) is TextBlock tb)
            {
                tb.Text = text;
            }
        }

        private void SetItemsSource(string elementName, object items)
        {
            if (FindName(elementName) is ItemsControl ic)
            {
                ic.ItemsSource = items as System.Collections.IEnumerable;
            }
        }

        private void LoadInvoiceData()
        {
            SetText("InvoiceDateText", _invoiceDate.ToString("dd/MM/yyyy"));
            SetText("InvoiceNumberText", _invoiceId.ToString());
            SetText("InvoiceForText", "Giao dịch bán hàng");

            // Set customer information
            SetText("CustomerNameText", string.IsNullOrWhiteSpace(_customer?.Name) ? "Khách lẻ" : _customer!.Name);
            SetText("CustomerPhoneText", string.IsNullOrWhiteSpace(_customer?.Phone) ? "Không có" : _customer!.Phone);
            SetText("CustomerEmailText", "Không có"); // CustomerListItem doesn't have Email property
            SetText("CustomerAddressText", "Không có"); // CustomerListItem doesn't have Address property

            SetItemsSource("InvoiceItemsList", _items);

            // Set totals
            SetText("SubtotalText", _subtotal.ToString("C"));
            SetText("TaxRateText", _taxPercent.ToString("F2") + "%");
            SetText("SalesTaxText", _taxAmount.ToString("C"));
            SetText("OtherText", _discount.ToString("C"));
            SetText("TotalText", _total.ToString("C"));

            GeneratePaymentQRCode(_invoiceId, _total);
        }

        private void LoadFromDatabase(int invoiceId, int employeeId)
        {
            try
            {
                
                var (header, items) = DatabaseHelper.GetInvoiceDetails(invoiceId);
                _invoiceHeader = header; // Lưu header để sử dụng trong CreateInvoiceContent
                

                // Header
                SetText("InvoiceDateText", header.CreatedDate.ToString("dd/MM/yyyy"));
                SetText("InvoiceTimeText", header.CreatedDate.ToString("HH:mm"));
                SetText("InvoiceNumberText", header.Id.ToString());
                SetText("InvoiceForText", "Giao dịch bán hàng");

                try
                {
                    var accounts = DatabaseHelper.GetAllAccounts();
                    // Sử dụng EmployeeId từ header thay vì từ tham số
                    var employee = accounts.FirstOrDefault(a => a.Id == header.EmployeeId);
                    SetText("EmployeeNameText", employee != default 
                        ? (string.IsNullOrWhiteSpace(employee.EmployeeName) ? employee.Username : employee.EmployeeName)
                        : "Không xác định");
                }
                catch
                {
                    SetText("EmployeeNameText", "Không xác định");
                }

                // Customer
                SetText("CustomerNameText", string.IsNullOrWhiteSpace(header.CustomerName) ? "Khách lẻ" : header.CustomerName);
                SetText("CustomerPhoneText", string.IsNullOrWhiteSpace(header.CustomerPhone) ? "Không có" : header.CustomerPhone);
                SetText("CustomerEmailText", string.IsNullOrWhiteSpace(header.CustomerEmail) ? "Không có" : header.CustomerEmail);
                SetText("CustomerAddressText", string.IsNullOrWhiteSpace(header.CustomerAddress) ? "Không có" : header.CustomerAddress);

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
                
                
                SetItemsSource("InvoiceItemsList", vmItems);

                // Totals
                SetText("SubtotalText", header.Subtotal.ToString("C"));
                var taxPercent = header.Subtotal == 0 ? 0m : (header.TaxAmount / header.Subtotal * 100m);
                SetText("TaxRateText", taxPercent.ToString("F2") + "%");
                SetText("SalesTaxText", header.TaxAmount.ToString("C"));
                SetText("OtherText", header.Discount.ToString("C"));
                SetText("TotalText", header.Total.ToString("C"));
                SetText("PaidText", header.Paid.ToString("C"));

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
                    var preview = FindName("InvoicePreviewRoot") as FrameworkElement;
                    var toPrint = preview ?? CreatePrintVisual();

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
            var printGrid = new Grid { Width = 1000, Height = 1400, Background = Brushes.White };
            printGrid.Children.Add(CreateInvoiceContent());
            return printGrid;
        }

        private FrameworkElement CreateInvoiceContent()
        {
            var mainGrid = new Grid();
            mainGrid.Width = 1000;
            mainGrid.Margin = new Thickness(60);

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
            companyStack.Children.Add(new TextBlock { Text = "HỆ THỐNG QUẢN LÝ BÁN HÀNG", FontSize = 26, FontWeight = FontWeights.Bold });
            
            // Thêm thông tin nhân viên
            try
            {
                var accounts = DatabaseHelper.GetAllAccounts();
                // Sử dụng EmployeeId từ header nếu có, nếu không thì dùng _employeeId
                int employeeIdToUse = _invoiceHeader?.EmployeeId ?? _employeeId;
                var employee = accounts.FirstOrDefault(a => a.Id == employeeIdToUse);
                string employeeName = "Không xác định";
                if (employee != default)
                {
                    employeeName = string.IsNullOrWhiteSpace(employee.EmployeeName) ? employee.Username : employee.EmployeeName;
                }
                companyStack.Children.Add(new TextBlock { Text = $"Nhân viên: {employeeName}", FontSize = 14, Margin = new Thickness(0, 5, 0, 0) });
            }
            catch
            {
                companyStack.Children.Add(new TextBlock { Text = "Nhân viên: Không xác định", FontSize = 14, Margin = new Thickness(0, 5, 0, 0) });
            }

            Grid.SetColumn(companyStack, 0);
            headerGrid.Children.Add(companyStack);

            // Invoice Title
            var invoiceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            invoiceStack.Children.Add(new TextBlock { Text = "HÓA ĐƠN", FontSize = 32, FontWeight = FontWeights.Bold });
            invoiceStack.Children.Add(CreateInfoRow("Ngày:", _invoiceDate.ToString("dd/MM/yyyy")));
            invoiceStack.Children.Add(CreateInfoRow("Số HĐ:", _invoiceId.ToString()));
            Grid.SetColumn(invoiceStack, 1);
            headerGrid.Children.Add(invoiceStack);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Customer Info
            var customerStack = new StackPanel { Margin = new Thickness(0, 20, 0, 20) };
            customerStack.Children.Add(new TextBlock { Text = "KHÁCH HÀNG:", FontWeight = FontWeights.Bold, FontSize = 16 });
            customerStack.Children.Add(new TextBlock { Text = _customer?.Name ?? "Khách lẻ", FontSize = 14, Margin = new Thickness(0, 5, 0, 0) });
            if (_customer != null && !string.IsNullOrWhiteSpace(_customer.Phone))
                customerStack.Children.Add(new TextBlock { Text = _customer.Phone, FontSize = 14 });
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
            stack.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, Width = 80, FontSize = 16 });
            stack.Children.Add(new TextBlock { Text = value, FontSize = 16 });
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
            
            header.Children.Add(new TextBlock { Text = "Sản phẩm", FontWeight = FontWeights.Bold, Margin = new Thickness(8), FontSize = 14 });
            var qty = new TextBlock { Text = "SL", FontWeight = FontWeights.Bold, Margin = new Thickness(8), FontSize = 14 };
            Grid.SetColumn(qty, 1);
            header.Children.Add(qty);
            var price = new TextBlock { Text = "Đơn giá", FontWeight = FontWeights.Bold, Margin = new Thickness(8), FontSize = 14 };
            Grid.SetColumn(price, 2);
            header.Children.Add(price);
            var total = new TextBlock { Text = "Thành tiền", FontWeight = FontWeights.Bold, Margin = new Thickness(8), FontSize = 14 };
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
                
                row.Children.Add(new TextBlock { Text = item.ProductName, Margin = new Thickness(8), FontSize = 13 });
                var itemQty = new TextBlock { Text = item.Quantity.ToString(), Margin = new Thickness(8), FontSize = 13 };
                Grid.SetColumn(itemQty, 1);
                row.Children.Add(itemQty);
                var itemPrice = new TextBlock { Text = item.UnitPrice.ToString("C"), Margin = new Thickness(8), FontSize = 13 };
                Grid.SetColumn(itemPrice, 2);
                row.Children.Add(itemPrice);
                var itemTotal = new TextBlock { Text = item.LineTotal.ToString("C"), Margin = new Thickness(8), FontSize = 13 };
                Grid.SetColumn(itemTotal, 3);
                row.Children.Add(itemTotal);
                
                table.Children.Add(row);
            }
            
            return table;
        }

        private Grid CreateTotalsSection()
        {
            var totalsGrid = new Grid { Margin = new Thickness(0, 20, 0, 0) };
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });

            var notesStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            notesStack.Children.Add(new TextBlock { Text = "Cảm ơn quý khách đã sử dụng dịch vụ!", FontSize = 14 });
            Grid.SetColumn(notesStack, 0);
            totalsGrid.Children.Add(notesStack);

            // Right side - Totals
            var totalsStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            var totalsTable = new StackPanel();
            
            totalsTable.Children.Add(new TextBlock { Text = $"Tạm tính: {_subtotal:C}", HorizontalAlignment = HorizontalAlignment.Right, FontSize = 14, Margin = new Thickness(0, 2, 0, 2) });
            totalsTable.Children.Add(new TextBlock { Text = $"Thuế: {_taxAmount:C}", HorizontalAlignment = HorizontalAlignment.Right, FontSize = 14, Margin = new Thickness(0, 2, 0, 2) });
            totalsTable.Children.Add(new TextBlock { Text = $"Giảm giá: {_discount:C}", HorizontalAlignment = HorizontalAlignment.Right, FontSize = 14, Margin = new Thickness(0, 2, 0, 2) });
            totalsTable.Children.Add(new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 5, 0, 5) });
            totalsTable.Children.Add(new TextBlock { Text = $"Tổng cộng: {_total:C}", FontWeight = FontWeights.Bold, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 0) });

            totalsStack.Children.Add(totalsTable);
            Grid.SetColumn(totalsStack, 1);
            totalsGrid.Children.Add(totalsStack);

            return totalsGrid;
        }


        private void GeneratePaymentQRCode(int invoiceId, decimal total)
        {
            try
            {
                var paymentSettings = PaymentSettingsManager.Load();
                
                if (!paymentSettings.EnableQRCode)
                {
                    // Ẩn QR code nếu bị tắt
                    var paymentQRCode = FindName("PaymentQRCode") as Image;
                    if (paymentQRCode?.Parent is Border qrBorder)
                    {
                        qrBorder.Visibility = Visibility.Collapsed;
                    }
                    return;
                }

                // Sử dụng VietQR API để tạo QR code thanh toán ngân hàng
                var qrCodeImage = FindName("PaymentQRCode") as Image;
                if (qrCodeImage != null)
                {
                    // Tạo QR code nếu có thông tin ngân hàng được cấu hình
                    if (paymentSettings.BankAccount != null && paymentSettings.BankCode != null)
                    {
                        // Use INV prefix + invoice ID (max 5 digits) to keep it safe and clearly not an amount
                        string invoiceIdStr = invoiceId.ToString();
                        int invoiceDigitsToUse = Math.Min(invoiceIdStr.Length, 5);
                        string description = "INV" + invoiceIdStr.Substring(0, invoiceDigitsToUse);

                        // Đảm bảo description ngắn và an toàn, bắt đầu bằng chữ cái
                        description = System.Text.RegularExpressions.Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                        if (description.Length > 8)
                        {
                            description = description.Substring(0, 8);
                        }
                        
                        // Ensure it starts with letters and is safe from being confused with amounts
                        if (string.IsNullOrWhiteSpace(description) || char.IsDigit(description[0]))
                        {
                            description = "INVOICE";
                            if (invoiceIdStr.Length > 0)
                            {
                                description = "INV" + invoiceIdStr.Substring(0, Math.Min(5, invoiceIdStr.Length));
                            }
                            description = System.Text.RegularExpressions.Regex.Replace(description, @"[^a-zA-Z0-9]", "");
                            if (description.Length > 8) description = description.Substring(0, 8);
                        }

                        qrCodeImage.Source = QRCodeHelper.GenerateVietQRCode_Safe(paymentSettings.BankCode.ToLower(), paymentSettings.BankAccount, total, description, false, 370, paymentSettings.AccountHolder);
                    }
                    else
                    {
                        qrCodeImage.Source = null;
                        if (qrCodeImage.Parent is Border qrBorderHide)
                        {
                            qrBorderHide.Visibility = Visibility.Collapsed;
                        }
                    }
                    
                    // Hiển thị QR code container nếu bị ẩn
                    if (qrCodeImage.Parent is Border qrBorder)
                    {
                        qrBorder.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        // Đã chuyển sang sử dụng QRCodeHelper.GenerateQRByMethod() thay vì các method riêng lẻ
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
