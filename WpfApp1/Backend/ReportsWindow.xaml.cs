using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace WpfApp1
{
    public partial class ReportsWindow : Window
    {
        private List<InvoiceListItem> _invoices = new();
        private PaginationHelper<InvoiceListItem> _paginationHelper = new();

        public ReportsWindow()
        {
            InitializeComponent();
            _paginationHelper.PageChanged += OnPageChanged;
            LoadFilters();
            LoadInvoices();
            
            // Enable sorting for DataGrid
            InvoicesDataGrid.Sorting += InvoicesDataGrid_Sorting;
        }
        
        private void OnPageChanged()
        {
            UpdateDisplayAndPagination();
        }

        

        private void LoadFilters()
        {
            // Get oldest invoice date from database
            var (oldestDate, newestDate) = DatabaseHelper.GetInvoiceDateRange();
            
            // Set default date range: from oldest invoice to today
            ToDatePicker.SelectedDate = DateTime.Today;
            FromDatePicker.SelectedDate = oldestDate ?? DateTime.Today.AddYears(-1); // Fallback to 1 year ago if no invoices

            var customers = DatabaseHelper.GetAllCustomers();
            var list = new List<CustomerList> { new CustomerList { Id = 0, Name = "Tất cả khách hàng" } };
            list.AddRange(customers.ConvertAll(c => new CustomerList { Id = c.Id, Name = c.Name }));
            CustomerComboBox.ItemsSource = list;
            CustomerComboBox.SelectedIndex = 0;
        }

        private void LoadInvoices()
        {
            DateTime? from = FromDatePicker.SelectedDate;
            DateTime? to = ToDatePicker.SelectedDate?.AddDays(1).AddTicks(-1); // include end day
            int? customerId = (CustomerComboBox.SelectedValue as int?) ?? 0;
            string search = (SearchTextBox.Text ?? string.Empty).Trim();

            var data = DatabaseHelper.QueryInvoices(from, to, customerId == 0 ? null : customerId, search);
            _invoices = data.ConvertAll(i => new InvoiceListItem
            {
                Id = i.Id,
                CreatedDate = i.CreatedDate,
                CustomerName = i.CustomerName,
                Subtotal = i.Subtotal,
                TaxAmount = i.TaxAmount,
                Discount = i.Discount,
                Total = i.Total,
                Paid = i.Paid
            });
            _paginationHelper.SetData(_invoices);
            UpdateDisplayAndPagination();
            RefreshKPIStats();
            CountTextBlock.Text = _paginationHelper.TotalItems.ToString();
            StatusTextBlock.Text = _paginationHelper.TotalItems == 0 ? "Không tìm thấy hóa đơn nào với bộ lọc đã chọn." : string.Empty;
        }

        private void UpdateDisplayAndPagination()
        {
            // Update DataGrid with current page items
            InvoicesDataGrid.ItemsSource = _paginationHelper.GetCurrentPageItems();
            
            // Update pagination info
            if (ReportsPageInfoTextBlock != null)
            {
                ReportsPageInfoTextBlock.Text = $"📄 Trang: {_paginationHelper.GetPageInfo()} • 📊 Tổng: {_paginationHelper.TotalItems} hóa đơn";
            }
            
            // Update current page textbox
            if (ReportsCurrentPageTextBox != null)
            {
                ReportsCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
            }
            
            // Update button states
            if (ReportsFirstPageButton != null) ReportsFirstPageButton.IsEnabled = _paginationHelper.CanGoFirst;
            if (ReportsPrevPageButton != null) ReportsPrevPageButton.IsEnabled = _paginationHelper.CanGoPrevious;
            if (ReportsNextPageButton != null) ReportsNextPageButton.IsEnabled = _paginationHelper.CanGoNext;
            if (ReportsLastPageButton != null) ReportsLastPageButton.IsEnabled = _paginationHelper.CanGoLast;
        }

        private void RefreshKPIStats()
        {
            try
            {
                // Get today's data
                var today = DateTime.Today;
                var todayInvoices = DatabaseHelper.QueryInvoices(today, today, null, string.Empty);
                var todayRevenue = todayInvoices.Sum(i => i.Total);
                
                // Get 30 days data  
                var thirtyDaysAgo = today.AddDays(-30);
                var monthInvoices = DatabaseHelper.QueryInvoices(thirtyDaysAgo, today, null, string.Empty);
                var monthRevenue = monthInvoices.Sum(i => i.Total);
                
                // Get total customers and products
                var totalCustomers = DatabaseHelper.GetTotalCustomers();
                var totalProducts = DatabaseHelper.GetTotalProducts();

   
                    
                if (TotalInvoicesText != null)
                    TotalInvoicesText.Text = $"{_paginationHelper.TotalItems} hóa đơn";
                    
                if (RevenueTextBlock != null)
                    RevenueTextBlock.Text = $"{_invoices.Sum(x => x.Total):N2}₫";
                    
                // Update last refresh time
                if (LastUpdateText != null)
                    LastUpdateText.Text = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            }
            catch
            {
                // Silent failure
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            LoadInvoices();
        }

       
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadInvoices();
        }
        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void Last7DaysButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void Last30DaysButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void ThisMonthButton_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            FromDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }
        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ReportsSettingsWindow();
            try
            {
                settings.Owner = this;
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            catch (InvalidOperationException)
            {
                settings.Owner = null;
                settings.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            settings.ShowDialog();
        }

        




        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            if (_invoices.Count == 0)
            {
                MessageBox.Show("Không có gì để xuất.", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var downloads = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = System.IO.Path.Combine(downloads, $"invoices_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            bool success = DatabaseHelper.ExportInvoicesToCsv(path);
            if (success)
            {
                MessageBox.Show($"Đã xuất {_invoices.Count} hóa đơn (bao gồm chi tiết items) đến:\n{path}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Không thể xuất dữ liệu. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn file CSV để nhập",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    ImportCsvFile(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi nhập file CSV: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportCsvFile(string filePath)
        {
            try
            {
                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show("File CSV không tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int importedCount = DatabaseHelper.ImportInvoicesFromCsv(filePath);
                
                if (importedCount > 0)
                {
                    MessageBox.Show($"Đã nhập thành công {importedCount} hóa đơn (bao gồm chi tiết items) từ file CSV.", "Nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the list
                    LoadInvoices();
                    
                    // Trigger dashboard refresh
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else if (importedCount == 0)
                {
                    MessageBox.Show("Không có hóa đơn nào được nhập từ file CSV.\n\nVui lòng kiểm tra:\n1. File CSV có đúng format export từ hệ thống này không\n2. File có dữ liệu items (các dòng bắt đầu bằng ITEM)\n3. ProductId trong file CSV phải tồn tại trong database\n4. Định dạng file CSV đúng\n\nXem Output Window để biết chi tiết lỗi.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Không thể nhập hóa đơn từ file CSV.\n\nNguyên nhân có thể:\n1. File CSV đang được mở bởi Excel/ứng dụng khác (vui lòng đóng file)\n2. Định dạng file CSV không đúng\n3. Sản phẩm không tồn tại trong database\n4. Lỗi kết nối database\n\nVui lòng xem Output Window để biết chi tiết lỗi.", "Lỗi nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhập file CSV:\n{ex.Message}\n\nChi tiết:\n{ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Old import method kept for backwards compatibility
        private void ImportCsvFileOld(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length <= 1)
            {
                MessageBox.Show("File CSV trống hoặc chỉ có header.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Xác nhận trước khi import
            var result = MessageBox.Show(
                $"Bạn có chắc muốn nhập {lines.Length - 1} hóa đơn từ file CSV?\n\nLưu ý: Dữ liệu sẽ được thêm vào cơ sở dữ liệu hiện tại.",
                "Xác nhận nhập dữ liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            // Lấy current user để làm employee
            var currentUser = Application.Current.Resources["CurrentUser"] as string;
            int employeeId = DatabaseHelper.GetEmployeeIdByUsername(currentUser ?? "admin");

            // Skip header (line 0)
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < 8)
                    {
                        errors.Add($"Dòng {i + 1}: Không đủ cột dữ liệu (cần 8 cột)");
                        errorCount++;
                        continue;
                    }

                    // Parse CSV fields: Id,Date,Customer,Subtotal,Tax,Discount,Total,Paid
                    var dateStr = fields[1].Trim('"');
                    var customerName = fields[2].Trim('"');
                    var subtotal = decimal.Parse(fields[3]);
                    var taxAmount = decimal.Parse(fields[4]);
                    var discount = decimal.Parse(fields[5]);
                    var total = decimal.Parse(fields[6]);
                    var paid = decimal.Parse(fields[7]);

                    // Parse date from CSV file
                    DateTime invoiceDate;
                    if (!DateTime.TryParse(dateStr, out invoiceDate))
                    {
                        // Fallback to current time if date parsing fails
                        invoiceDate = DateTime.Now;
                        errors.Add($"Dòng {i + 1}: Không thể parse thời gian '{dateStr}', sử dụng thời gian hiện tại");
                    }

                    // Tìm hoặc tạo khách hàng
                    var customers = DatabaseHelper.GetAllCustomers();
                    var existingCustomer = customers.FirstOrDefault(c => c.Name.Equals(customerName, StringComparison.OrdinalIgnoreCase));
                    
                    int customerId;
                    if (existingCustomer.Id == 0)
                    {
                        // Tạo khách hàng mới
                        bool customerCreated = DatabaseHelper.AddCustomer(customerName, "", "", "Regular", "");
                        if (!customerCreated)
                        {
                            errors.Add($"Dòng {i + 1}: Không thể tạo khách hàng '{customerName}'");
                            errorCount++;
                            continue;
                        }
                        
                        // Lấy lại customer vừa tạo
                        var newCustomers = DatabaseHelper.GetAllCustomers();
                        var newCustomer = newCustomers.FirstOrDefault(c => c.Name.Equals(customerName, StringComparison.OrdinalIgnoreCase));
                        customerId = newCustomer.Id;
                        
                        if (customerId <= 0)
                        {
                            errors.Add($"Dòng {i + 1}: Không thể lấy ID khách hàng '{customerName}'");
                            errorCount++;
                            continue;
                        }
                    }
                    else
                    {
                        customerId = existingCustomer.Id;
                    }

                    // Tính toán tax percent
                    decimal taxPercent = subtotal > 0 ? (taxAmount / subtotal) * 100 : 0;

                    // Tạo hóa đơn với 1 item dummy (vì SaveInvoice yêu cầu items)
                    var items = new List<(int ProductId, int Quantity, decimal UnitPrice)>
                    {
                        (1, 1, subtotal) // Sử dụng product ID 1 với quantity 1 và unit price = subtotal
                    };

                    bool success = DatabaseHelper.SaveInvoice(
                        customerId, employeeId, subtotal, taxPercent, taxAmount,
                        discount, total, paid, items, invoiceDate);

                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        errors.Add($"Dòng {i + 1}: Lỗi lưu hóa đơn vào database");
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {i + 1}: {ex.Message}");
                    errorCount++;
                }
            }

            // Hiển thị kết quả
            var message = new StringBuilder();
            message.AppendLine($"Kết quả nhập CSV:");
            message.AppendLine($"✅ Thành công: {successCount} hóa đơn");
            message.AppendLine($"❌ Lỗi: {errorCount} hóa đơn");

            if (errors.Count > 0 && errors.Count <= 10)
            {
                message.AppendLine("\nChi tiết lỗi:");
                foreach (var error in errors)
                {
                    message.AppendLine($"• {error}");
                }
            }
            else if (errors.Count > 10)
            {
                message.AppendLine($"\nCó {errors.Count} lỗi. Hiển thị 10 lỗi đầu tiên:");
                for (int i = 0; i < 10; i++)
                {
                    message.AppendLine($"• {errors[i]}");
                }
            }

            MessageBox.Show(message.ToString(), "Kết quả nhập CSV", 
                MessageBoxButton.OK, 
                errorCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

            // Refresh data nếu có ít nhất 1 hóa đơn thành công
            if (successCount > 0)
            {
                LoadInvoices();
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString());
            return result.ToArray();
        }

        // Pagination event handlers
        private void ReportsFirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.FirstPage();
        }

        private void ReportsPrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.PreviousPage();
        }

        private void ReportsNextPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.NextPage();
        }

        private void ReportsLastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.LastPage();
        }

        private void ReportsCurrentPageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(ReportsCurrentPageTextBox.Text, out int pageNumber))
                {
                    if (!_paginationHelper.GoToPage(pageNumber))
                    {
                        // Reset to current page if invalid
                        ReportsCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                        MessageBox.Show($"Trang không hợp lệ. Vui lòng nhập từ 1 đến {_paginationHelper.TotalPages}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    ReportsCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                }
            }
        }
        
        private void InvoicesDataGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
        {
            e.Handled = true; // Prevent default sorting
            
            var column = e.Column;
            var propertyName = column.SortMemberPath;
            
            if (string.IsNullOrEmpty(propertyName)) 
            {
                return;
            }
            
            // Determine sort direction
            var direction = column.SortDirection != System.ComponentModel.ListSortDirection.Ascending 
                ? System.ComponentModel.ListSortDirection.Ascending 
                : System.ComponentModel.ListSortDirection.Descending;
            
            // Apply sort to all data through PaginationHelper
            Func<IEnumerable<InvoiceListItem>, IOrderedEnumerable<InvoiceListItem>>? sortFunc = null;
            
            switch (propertyName.ToLower())
            {
                case "id":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.Id)
                        : items => items.OrderByDescending(i => i.Id);
                    break;
                case "createddate":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.CreatedDate)
                        : items => items.OrderByDescending(i => i.CreatedDate);
                    break;
                case "customername":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.CustomerName)
                        : items => items.OrderByDescending(i => i.CustomerName);
                    break;
                case "subtotal":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.Subtotal)
                        : items => items.OrderByDescending(i => i.Subtotal);
                    break;
                case "taxamount":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.TaxAmount)
                        : items => items.OrderByDescending(i => i.TaxAmount);
                    break;
                case "discount":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.Discount)
                        : items => items.OrderByDescending(i => i.Discount);
                    break;
                case "total":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.Total)
                        : items => items.OrderByDescending(i => i.Total);
                    break;
                case "paid":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(i => i.Paid)
                        : items => items.OrderByDescending(i => i.Paid);
                    break;
            }
            
            if (sortFunc != null)
            {
                _paginationHelper.SetSort(sortFunc);
                
                // Update column sort direction
                column.SortDirection = direction;
                
                // Clear other columns' sort direction
                foreach (var col in InvoicesDataGrid.Columns)
                {
                    if (col != column)
                        col.SortDirection = null;
                }
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceListItem row)
            {
                var detail = DatabaseHelper.GetInvoiceDetails(row.Id);
                var sb = new StringBuilder();
                sb.AppendLine($"Hóa đơn #{detail.Header.Id} - {detail.Header.CreatedDate:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"Khách hàng: {detail.Header.CustomerName}");
                sb.AppendLine("Sản phẩm:");
                foreach (var it in detail.Items)
                {
                    sb.AppendLine($" - {it.ProductName} x{it.Quantity} @ {it.UnitPrice:F2} = {it.LineTotal:F2}");
                }
                sb.AppendLine($"Tạm tính: {detail.Header.Subtotal:F2}");
                sb.AppendLine($"Thuế: {detail.Header.TaxAmount:F2}");
                sb.AppendLine($"Giảm giá: {detail.Header.Discount:F2}");
                sb.AppendLine($"Tổng cộng: {detail.Header.Total:F2}");
                sb.AppendLine($"Đã trả: {detail.Header.Paid:F2}");
                MessageBox.Show(sb.ToString(), $"Hóa đơn #{detail.Header.Id}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceListItem row)
            {
                var confirm = MessageBox.Show($"Xóa hóa đơn #{row.Id}?\nHành động này không thể hoàn tác.", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    if (DatabaseHelper.DeleteInvoice(row.Id))
                    {
                        LoadInvoices();
                        MessageBox.Show($"Hóa đơn #{row.Id} đã được xóa.", "Đã xóa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Xóa thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private int GetEmployeeId(string username)
        {
            return DatabaseHelper.GetEmployeeIdByUsername(username);
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceListItem row)
            {
                var printWindow = new InvoicePrintWindow(row.Id, 1);
                printWindow.ShowDialog();
            }
        }
    }

    public class InvoiceListItem
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
    }

    public class CustomerList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
