using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            // Date range defaults: show all invoices (no date filter)
            ToDatePicker.SelectedDate = null;
            FromDatePicker.SelectedDate = null;

            var customers = DatabaseHelper.GetAllCustomers();
            var list = new List<CustomerList> { new CustomerList { Id = 0, Name = "Tất cả khách hàng" } };
            list.AddRange(customers.ConvertAll(c => new CustomerList { Id = c.Id, Name = c.Name }));
            CustomerComboBox.ItemsSource = list;
            CustomerComboBox.SelectedIndex = 0;
        }

        private void LoadInvoices()
        {
            try
            {
                DateTime? from = FromDatePicker.SelectedDate;
                DateTime? to = ToDatePicker.SelectedDate?.AddDays(1).AddTicks(-1); // include end day
                int? customerId = (CustomerComboBox.SelectedValue as int?) ?? 0;
                string search = (SearchTextBox.Text ?? string.Empty).Trim();

                System.Diagnostics.Debug.WriteLine($"LoadInvoices: From={from}, To={to}, CustomerId={customerId}, Search='{search}'");

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

                System.Diagnostics.Debug.WriteLine($"Found {_invoices.Count} invoices");

                _paginationHelper.SetData(_invoices);
                UpdateDisplayAndPagination();
                
                // Update summary information
                CountTextBlock.Text = _paginationHelper.TotalItems.ToString();
                RevenueTextBlock.Text = _invoices.Sum(x => x.Total).ToString("F2") + "₫";
                
                // Update TotalInvoicesText as well
                if (TotalInvoicesText != null)
                {
                    TotalInvoicesText.Text = $"{_paginationHelper.TotalItems} hóa đơn";
                }
                
                // Update status
                StatusTextBlock.Text = _paginationHelper.TotalItems == 0 ? "Không tìm thấy hóa đơn nào với bộ lọc đã chọn." : $"Báo cáo được cập nhật lúc: {DateTime.Now:HH:mm:ss}";

                LoadCharts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadInvoices: {ex.Message}");
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            LoadInvoices();
        }

        private void LoadCharts()
        {
            var to = ToDatePicker.SelectedDate ?? DateTime.Today;
            var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);

            // Revenue trend
            var revenue = DatabaseHelper.GetRevenueByDay(from, to);
            var revenueModel = new PlotModel { Title = null };
            revenueModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM-dd", IsZoomEnabled = false, IsPanEnabled = false });
            revenueModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, IsZoomEnabled = false, IsPanEnabled = false });
            var line = new LineSeries { MarkerType = MarkerType.Circle };
            foreach (var (day, amount) in revenue)
            {
                line.Points.Add(new DataPoint(DateTimeAxis.ToDouble(day), (double)amount));
            }
            revenueModel.Series.Add(line);
            RevenuePlot.Model = revenueModel;

            // Top products
            var top = DatabaseHelper.GetTopProducts(from, to, 10);
            var barModel = new PlotModel { Title = null };
            var catAxis = new CategoryAxis { Position = AxisPosition.Left };
            foreach (var (name, _) in top)
            {
                catAxis.Labels.Add(name);
            }
            barModel.Axes.Add(catAxis);
            barModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0 });
            var barSeries = new BarSeries();
            foreach (var (_, qty) in top)
            {
                barSeries.Items.Add(new BarItem { Value = qty });
            }
            barModel.Series.Add(barSeries);
            TopProductsPlot.Model = barModel;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadInvoices();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("TodayButton_Click called");
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void Last7DaysButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Last7DaysButton_Click called");
            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void Last30DaysButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Last30DaysButton_Click called");
            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            ToDatePicker.SelectedDate = DateTime.Today;
            LoadInvoices();
        }

        private void ThisMonthButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ThisMonthButton_Click called");
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

        private void ShowChartsButton_Click(object sender, RoutedEventArgs e)
        {
            // Simply ensure charts reflect current filters
            LoadCharts();
        }

        private void ShowReportsButton_Click(object sender, RoutedEventArgs e)
        {
            // Refresh data grid and KPIs based on current filters
            LoadInvoices();
        }

        private void LoadChartsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCharts();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            if (_invoices.Count == 0)
            {
                MessageBox.Show("Không có gì để xuất.", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Customer,Subtotal,Tax,Discount,Total,Paid");
            foreach (var i in _invoices)
            {
                sb.AppendLine($"{i.Id},\"{i.CreatedDate:yyyy-MM-dd HH:mm}\",\"{i.CustomerName}\",{i.Subtotal:F2},{i.TaxAmount:F2},{i.Discount:F2},{i.Total:F2},{i.Paid:F2}");
            }

            var downloads = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = System.IO.Path.Combine(downloads, $"invoices_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            // Ghi UTF-8 không BOM theo yêu cầu
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            MessageBox.Show($"Đã xuất đến {path}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
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
            
            // Debug: Show what property we're trying to sort by
            System.Diagnostics.Debug.WriteLine($"Sorting by: '{propertyName}'");
            
            if (string.IsNullOrEmpty(propertyName)) 
            {
                System.Diagnostics.Debug.WriteLine("SortMemberPath is null or empty!");
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
                    System.Diagnostics.Debug.WriteLine("Sorting by ID");
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
