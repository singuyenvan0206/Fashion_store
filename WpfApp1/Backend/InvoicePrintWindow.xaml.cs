using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace WpfApp1
{
        public partial class InvoicePrintWindow : Window
        {
            private readonly List<InvoiceItemViewModel> _items;
            private readonly CustomerListItem _customer = new();
            private readonly decimal _subtotal;
            private readonly decimal _taxPercent;
            private readonly decimal _taxAmount;
            private readonly decimal _discount;
            private readonly decimal _total;
            private readonly int _invoiceId;
            private readonly DateTime _invoiceDate;

            // Thông tin bổ sung lấy từ database
            private string _employeeName = string.Empty;
            private string _customerAddress = string.Empty;
            private string _customerEmail = string.Empty;

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

        private void LoadInvoiceData()
        {
            // Set company information
            CompanyNameText.Text = "HỆ THỐNG QUẢN LÝ BÁN HÀNG";
            CompanySloganText.Text = "Giải pháp bán hàng chuyên nghiệp";
            CompanyAddressText.Text = ""; // Ẩn để đơn giản
            CompanyCityText.Text = "";    // Ẩn để đơn giản
            CompanyPhoneText.Text = "";   // Ẩn để đơn giản
            CompanyFaxText.Text = "";     // Ẩn để đơn giản

            // Set invoice information
            InvoiceDateText.Text = _invoiceDate.ToString("dd/MM/yyyy");
            InvoiceNumberText.Text = _invoiceId.ToString();
            InvoiceForText.Text = "Giao dịch bán hàng";

            // Set customer information
            CustomerNameText.Text = _customer?.Name ?? "Khách lẻ";
            CustomerPhoneText.Text = _customer?.Phone ?? string.Empty;

            // Load items
            InvoiceItemsList.ItemsSource = _items;

            // Set totals
            SubtotalText.Text = _subtotal.ToString("C");
            TaxRateText.Text = _taxPercent.ToString("F2") + "%";
            SalesTaxText.Text = _taxAmount.ToString("C");
            OtherText.Text = _discount.ToString("C");
            TotalText.Text = _total.ToString("C");
        }

<<<<<<< HEAD
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

                // Employee + Customer directly from database for accuracy
                try
                {
                    string connStr = SettingsManager.BuildConnectionString();
                    using var conn = new MySqlConnection(connStr);
                    conn.Open();
                    string sql = @"SELECT IFNULL(a.EmployeeName, a.Username) AS EmpName,
                                          IFNULL(c.Name, '') AS CustName,
                                          IFNULL(c.Phone, '') AS CustPhone,
                                          IFNULL(c.Address, '') AS CustAddress,
                                          IFNULL(c.Email, '') AS CustEmail
                                     FROM Invoices i
                                     LEFT JOIN Accounts a ON a.Id = i.EmployeeId
                                     LEFT JOIN Customers c ON c.Id = i.CustomerId
                                     WHERE i.Id = @id";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", invoiceId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        _employeeName = r.IsDBNull(0) ? string.Empty : r.GetString(0);
                        EmployeeNameText.Text = _employeeName;
                        CustomerNameText.Text = r.IsDBNull(1) ? string.Empty : r.GetString(1);
                        CustomerPhoneText.Text = r.IsDBNull(2) ? string.Empty : r.GetString(2);
                        _customerAddress = r.IsDBNull(3) ? string.Empty : r.GetString(3);
                        CustomerAddressText.Text = _customerAddress;
                        _customerEmail = r.IsDBNull(4) ? string.Empty : r.GetString(4);
                        CustomerEmailText.Text = _customerEmail;
                    }
                }
                catch { /* fallback below */ }
                
                // Fallbacks from header (in case above query fails)
                if (string.IsNullOrWhiteSpace(CustomerNameText.Text))
                    CustomerNameText.Text = header.CustomerName;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được dữ liệu hóa đơn #{invoiceId}: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

=======
>>>>>>> parent of 97921a3 (Change some layouts)
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                
                // Configure print settings
                printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);

                if (printDialog.ShowDialog() == true)
                {
                    // Create a visual for printing
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual, "Invoice #" + _invoiceId);
                    
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
            // Create a new Grid for printing
            var printGrid = new Grid();
            printGrid.Width = 800;
            printGrid.Background = Brushes.White;

            // Add the invoice content to the print grid
            var invoiceContent = CreateInvoiceContent();
            printGrid.Children.Add(invoiceContent);

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
            companyStack.Children.Add(new TextBlock
            {
                Text = "POS MANAGEMENT SYSTEM",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0, 0, 0, 5)
            });
            companyStack.Children.Add(new TextBlock
            {
                Text = "Professional Point of Sale Solutions",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                Margin = new Thickness(0, 0, 0, 10)
            });
            companyStack.Children.Add(new TextBlock
            {
                Text = "123 Business Street",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                Margin = new Thickness(0, 0, 0, 2)
            });
            companyStack.Children.Add(new TextBlock
            {
                Text = "Ho Chi Minh City, 700000",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                Margin = new Thickness(0, 0, 0, 2)
            });
            companyStack.Children.Add(new TextBlock
            {
                Text = "Phone: (028) 1234-5678",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                Margin = new Thickness(0, 0, 0, 2)
            });
            companyStack.Children.Add(new TextBlock
            {
                Text = "Fax: (028) 1234-5679",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94))
            });

            Grid.SetColumn(companyStack, 0);
            headerGrid.Children.Add(companyStack);

            // Invoice Title
            var invoiceStack = new StackPanel();
            invoiceStack.HorizontalAlignment = HorizontalAlignment.Right;
            invoiceStack.Children.Add(new TextBlock
            {
                Text = "INVOICE",
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 136, 229)),
                Margin = new Thickness(0, 0, 0, 20)
            });

            var invoiceInfoStack = new StackPanel();
            invoiceInfoStack.Children.Add(CreateInfoRow("DATE:", _invoiceDate.ToString("MMMM dd, yyyy")));
            invoiceInfoStack.Children.Add(CreateInfoRow("INVOICE #", _invoiceId.ToString()));
            invoiceInfoStack.Children.Add(CreateInfoRow("FOR:", "Sales Transaction"));

            invoiceStack.Children.Add(invoiceInfoStack);
            Grid.SetColumn(invoiceStack, 1);
            headerGrid.Children.Add(invoiceStack);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Billing Information (Name, Phone, Address, Email)
            var billingBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20),
                CornerRadius = new CornerRadius(5)
            };

            var billingStack = new StackPanel();
            billingStack.Children.Add(new TextBlock
            {
                Text = "BILL TO:",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0, 0, 0, 10)
            });
            billingStack.Children.Add(new TextBlock
            {
                Text = _customer?.Name ?? "Walk-in Customer",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            });
            billingStack.Children.Add(new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(_customer?.Phone) ? _customer!.Phone : string.Empty,
                FontSize = 12
            });
            billingStack.Children.Add(new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(_customerAddress) ? _customerAddress : string.Empty,
                FontSize = 12
            });
            billingStack.Children.Add(new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(_customerEmail) ? _customerEmail : string.Empty,
                FontSize = 12
            });
            billingStack.Children.Add(new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(_employeeName) ? $"Nhân viên: {_employeeName}" : string.Empty,
                FontSize = 12,
                FontStyle = FontStyles.Italic
            });

            billingBorder.Child = billingStack;
            Grid.SetRow(billingBorder, 1);
            mainGrid.Children.Add(billingBorder);

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
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                Width = 80
            });
            stack.Children.Add(new TextBlock { Text = value });
            return stack;
        }

        private FrameworkElement CreateItemsTable()
        {
            var tableBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var tableGrid = new Grid();

            // Table Header
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 136, 229)),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            headerGrid.Children.Add(new TextBlock
            {
                Text = "DESCRIPTION",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "QTY",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "UNIT PRICE",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "AMOUNT",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            Grid.SetColumn(headerGrid.Children[1], 1);
            Grid.SetColumn(headerGrid.Children[2], 2);
            Grid.SetColumn(headerGrid.Children[3], 3);

            headerBorder.Child = headerGrid;

            // Items
            var itemsStack = new StackPanel();
            foreach (var item in _items)
            {
                var itemBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(15, 10, 15, 10)
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.ProductName,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.Quantity.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.UnitPrice.ToString("F2"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.LineTotal.ToString("F2"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                });

                Grid.SetColumn(itemGrid.Children[1], 1);
                Grid.SetColumn(itemGrid.Children[2], 2);
                Grid.SetColumn(itemGrid.Children[3], 3);

                itemBorder.Child = itemGrid;
                itemsStack.Children.Add(itemBorder);
            }

            tableGrid.Children.Add(headerBorder);
            tableGrid.Children.Add(itemsStack);
            tableBorder.Child = tableGrid;

            return tableBorder;
        }

        private Grid CreateTotalsSection()
        {
            var totalsGrid = new Grid();
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Left side - Notes
            var notesStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            notesStack.Children.Add(new TextBlock
            {
                Text = "Make all checks payable to POS Management System",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                Margin = new Thickness(0, 0, 0, 20)
            });

            Grid.SetColumn(notesStack, 0);
            totalsGrid.Children.Add(notesStack);

            // Right side - Totals
            var totalsStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            var totalsTable = new Grid();
            totalsTable.Width = 280;

            // Add rows for totals
            var rows = new[]
            {
                ("SUBTOTAL:", _subtotal.ToString("C")),
                ("TAX RATE:", _taxPercent.ToString("F2") + "%"),
                ("SALES TAX:", _taxAmount.ToString("C")),
                ("OTHER:", _discount.ToString("C"))
            };

            for (int i = 0; i < rows.Length; i++)
            {
                totalsTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                rowGrid.Children.Add(new TextBlock
                {
                    Text = rows[i].Item1,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 10, 8)
                });
                rowGrid.Children.Add(new TextBlock
                {
                    Text = rows[i].Item2,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                Grid.SetColumn(rowGrid.Children[1], 1);
                Grid.SetRow(rowGrid, i);
                totalsTable.Children.Add(rowGrid);
            }

            // Total row with special styling
            totalsTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 136, 229)),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 20),
                CornerRadius = new CornerRadius(3)
            };

            var totalGrid = new Grid();
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            totalGrid.Children.Add(new TextBlock
            {
                Text = "TOTAL:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            });
            totalGrid.Children.Add(new TextBlock
            {
                Text = _total.ToString("C"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            Grid.SetColumn(totalGrid.Children[1], 1);
            totalBorder.Child = totalGrid;
            Grid.SetRow(totalBorder, rows.Length);
            totalsTable.Children.Add(totalBorder);

            totalsStack.Children.Add(totalsTable);
            Grid.SetColumn(totalsStack, 1);
            totalsGrid.Children.Add(totalsStack);

            return totalsGrid;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
