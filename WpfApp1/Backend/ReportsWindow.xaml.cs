using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace WpfApp1
{
    public partial class ReportsWindow : Window
    {
        private List<InvoiceListItem> _invoices = new();

        public ReportsWindow()
        {
            InitializeComponent();
            LoadFilters();
            LoadInvoices();
        }

        private void LoadFilters()
        {
            // Date range defaults: last 30 days
            ToDatePicker.SelectedDate = DateTime.Today;
            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);

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
            InvoicesDataGrid.ItemsSource = _invoices;
            CountTextBlock.Text = _invoices.Count.ToString();
            RevenueTextBlock.Text = _invoices.Sum(x => x.Total).ToString("F2");
            StatusTextBlock.Text = _invoices.Count == 0 ? "Không tìm thấy hóa đơn nào với bộ lọc đã chọn." : string.Empty;

            LoadCharts();
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

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow();
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

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceListItem row)
            {
                var detail = DatabaseHelper.GetInvoiceDetails(row.Id);
                
                // Convert invoice details to InvoiceItemViewModel list
                var items = detail.Items.Select(item => new InvoiceItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                }).ToList();

                // Create customer info
                var customer = new CustomerListItem
                {
                    Id = 0, // We don't have customer ID in the detail
                    Name = detail.Header.CustomerName
                };

                // Calculate tax percentage
                decimal taxPercent = 0;
                if (detail.Header.Subtotal > 0)
                {
                    taxPercent = (detail.Header.TaxAmount / detail.Header.Subtotal) * 100;
                }

                // Open the professional invoice print window
                var printWindow = new InvoicePrintWindow(
                    items, 
                    customer, 
                    detail.Header.Subtotal, 
                    taxPercent, 
                    detail.Header.TaxAmount, 
                    detail.Header.Discount, 
                    detail.Header.Total, 
                    detail.Header.Id, 
                    detail.Header.CreatedDate
                );
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
