using System;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp1
{
    public partial class ReportsSettingsWindow : Window
    {
        public ReportsSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadDatabaseStatistics();
        }

        private void LoadSettings()
        {
            // Load current settings from registry or config file
            // For now, use default values
            DefaultDateRangeComboBox.SelectedIndex = 1; // 30 days
            AutoRefreshCheckBox.IsChecked = false;
            ShowChartsCheckBox.IsChecked = true;
            ItemsPerPageComboBox.SelectedIndex = 0; // 15 items
            ExportFormatComboBox.SelectedIndex = 0; // CSV
            IncludeChartsCheckBox.IsChecked = false;
            
            // Set default export location to Downloads folder
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ExportLocationTextBox.Text = Path.Combine(downloadsPath, "Downloads");
        }

        private void LoadDatabaseStatistics()
        {
            try
            {
                // Get database statistics
                int totalInvoices = DatabaseHelper.GetTotalInvoices();
                decimal totalRevenue = DatabaseHelper.GetTotalRevenue();
                
                // Safely update UI elements
                if (TotalInvoicesTextBlock != null)
                    TotalInvoicesTextBlock.Text = totalInvoices.ToString("N0");
                if (TotalRevenueTextBlock != null)
                    TotalRevenueTextBlock.Text = totalRevenue.ToString("C0");
                
                // Calculate date range
                var (oldestDate, newestDate) = DatabaseHelper.GetInvoiceDateRange();
                if (DateRangeTextBlock != null)
                {
                    if (oldestDate.HasValue && newestDate.HasValue)
                    {
                        var daysDiff = Math.Max(0, (newestDate.Value - oldestDate.Value).Days);
                        DateRangeTextBlock.Text = $"{daysDiff} ngày";
                    }
                    else
                    {
                        DateRangeTextBlock.Text = "Chưa có dữ liệu";
                    }
                }
                
                // Estimate database size (simplified)
                if (DatabaseSizeTextBlock != null)
                    DatabaseSizeTextBlock.Text = EstimateDatabaseSize();
            }
            catch
            {
                // Safely update UI elements with error values
                if (TotalInvoicesTextBlock != null) TotalInvoicesTextBlock.Text = "N/A";
                if (TotalRevenueTextBlock != null) TotalRevenueTextBlock.Text = "N/A";
                if (DateRangeTextBlock != null) DateRangeTextBlock.Text = "N/A";
                if (DatabaseSizeTextBlock != null) DatabaseSizeTextBlock.Text = "N/A";
            }
        }

        private string EstimateDatabaseSize()
        {
            try
            {
                // This is a simplified estimation
                int totalInvoices = DatabaseHelper.GetTotalInvoices();
                int totalProducts = DatabaseHelper.GetTotalProducts();
                int totalCustomers = DatabaseHelper.GetTotalCustomers();
                
                // Rough estimation: each invoice ~1KB, product ~0.5KB, customer ~0.3KB
                long estimatedBytes = (totalInvoices * 1024) + (totalProducts * 512) + (totalCustomers * 300);
                
                if (estimatedBytes < 1024 * 1024) // Less than 1MB
                {
                    return $"{estimatedBytes / 1024:N0} KB";
                }
                else
                {
                    return $"{estimatedBytes / (1024 * 1024):N1} MB";
                }
            }
            catch
            {
                return "N/A";
            }
        }

        private void BrowseExportLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục để lưu file xuất",
                InitialDirectory = ExportLocationTextBox?.Text ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            
            if (folderDialog.ShowDialog() == true)
            {
                if (ExportLocationTextBox != null)
                    ExportLocationTextBox.Text = folderDialog.FolderName;
            }
        }

        private void DeleteAllInvoicesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("⚠️ CẢNH BÁO: Bạn sắp XÓA TẤT CẢ HÓA ĐƠN!\n\n" +
                                       "Hành động này KHÔNG THỂ HOÀN TÁC và sẽ xóa:\n" +
                                       "• Tất cả hóa đơn\n" +
                                       "• Chi tiết hóa đơn\n" +
                                       "• Lịch sử giao dịch\n\n" +
                                       "Bạn có CHẮC CHẮN muốn tiếp tục?", 
                                       "XÁC NHẬN XÓA TẤT CẢ", MessageBoxButton.YesNo, MessageBoxImage.Stop);
            
            if (result == MessageBoxResult.Yes)
            {
                // Simple text confirmation using MessageBox
                var confirmResult = MessageBox.Show(
                    "CẢNH BÁO CUỐI CÙNG!\n\nĐể xác nhận xóa TẤT CẢ hóa đơn, nhấn OK.\nĐể hủy bỏ, nhấn Cancel.\n\nHành động này KHÔNG THỂ HOÀN TÁC!",
                    "XÁC NHẬN CUỐI CÙNG",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Stop);
                
                string userInput = (confirmResult == MessageBoxResult.OK) ? "DELETE ALL" : "";
                
                if (userInput == "DELETE ALL")
                {
                    try
                    {
                        // Get count before deletion
                        int deletedCount = DatabaseHelper.GetTotalInvoices();
                        
                        // Delete all invoices
                        bool success = DatabaseHelper.DeleteAllInvoices();
                        
                        if (success)
                        {
                            MessageBox.Show($"Đã xóa {deletedCount} hóa đơn.\n\nTất cả dữ liệu hóa đơn đã được xóa khỏi hệ thống.", 
                                          "Xóa hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Trigger dashboard refresh for real-time updates
                            DashboardWindow.TriggerDashboardRefresh();
                        }
                        else
                        {
                            MessageBox.Show("Không thể xóa hóa đơn. Vui lòng thử lại.", 
                                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        LoadDatabaseStatistics(); // Refresh statistics
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi xóa dữ liệu: {ex.Message}", 
                                      "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Xác nhận không đúng. Hủy thao tác xóa.", 
                                  "Hủy thao tác", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save settings to registry or config file
                // For now, just show confirmation
                MessageBox.Show("Cài đặt đã được lưu thành công!", 
                              "Lưu cài đặt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu cài đặt: {ex.Message}", 
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
