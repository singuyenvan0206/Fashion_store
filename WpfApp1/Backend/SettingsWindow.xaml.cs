using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var cfg = SettingsManager.Load();
            ServerTextBox.Text = cfg.Server;
            DatabaseTextBox.Text = cfg.Database;
            UserIdTextBox.Text = cfg.UserId;
            PasswordBox.Password = cfg.Password;
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var cfg = new SettingsConfig
            {
                Server = ServerTextBox.Text.Trim(),
                Database = DatabaseTextBox.Text.Trim(),
                UserId = UserIdTextBox.Text.Trim(),
                Password = PasswordBox.Password
            };

            bool ok = SettingsManager.TestConnection(cfg, out string message);
            StatusTextBlock.Text = message;
            MessageBox.Show(message, ok ? "Thành công" : "Lỗi", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var cfg = new SettingsConfig
            {
                Server = ServerTextBox.Text.Trim(),
                Database = DatabaseTextBox.Text.Trim(),
                UserId = UserIdTextBox.Text.Trim(),
                Password = PasswordBox.Password
            };

            if (SettingsManager.Save(cfg, out string error))
            {
                StatusTextBlock.Text = "Cài đặt đã được lưu. Khởi động lại ứng dụng để áp dụng.";
                MessageBox.Show("Cài đặt đã được lưu. Vui lòng khởi động lại ứng dụng để áp dụng kết nối cơ sở dữ liệu mới.", "Đã lưu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(error, "Lưu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa tất cả dữ liệu mẫu?\n\n" +
                "Hành động này sẽ xóa:\n" +
                "• Tất cả danh mục\n" +
                "• Tất cả sản phẩm\n" +
                "• Tất cả khách hàng\n" +
                "• Tất cả hóa đơn\n\n" +
                "Dữ liệu sẽ được tạo lại khi khởi động ứng dụng lần sau.",
                "Xác nhận xóa dữ liệu mẫu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    #if DEBUG
                    DatabaseHelper.ClearSampleData();
                    #else
                    MessageBox.Show("Chức năng này chỉ khả dụng ở bản Debug.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                    #endif
                    MessageBox.Show(
                        "Đã xóa thành công tất cả dữ liệu mẫu.\n\n" +
                        "Dữ liệu mẫu sẽ được tạo lại khi bạn khởi động lại ứng dụng.",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Lỗi khi xóa dữ liệu mẫu:\n\n{ex.Message}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CheckStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #if DEBUG
                string status = DatabaseHelper.GetDatabaseStatus();
                #else
                string status = "Trạng thái database chỉ khả dụng ở bản Debug.";
                #endif
                
                // Tạo window hiển thị trạng thái
                var statusWindow = new Window
                {
                    Title = "Trạng Thái Database",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(20)
                };

                var textBlock = new TextBlock
                {
                    Text = status,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.Black
                };

                scrollViewer.Content = textBlock;
                statusWindow.Content = scrollViewer;
                statusWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kiểm tra trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDataNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #if DEBUG
                string result = DatabaseHelper.CreateSampleDataNow();
                #else
                string result = "Tạo dữ liệu mẫu chỉ khả dụng ở bản Debug.";
                #endif
                
                // Tạo window hiển thị kết quả chi tiết
                var resultWindow = new Window
                {
                    Title = "Kết Quả Tạo Dữ Liệu Mẫu",
                    Width = 700,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(20)
                };

                var textBlock = new TextBlock
                {
                    Text = result,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.Black
                };

                scrollViewer.Content = textBlock;
                resultWindow.Content = scrollViewer;
                resultWindow.ShowDialog();

                // Hiển thị thông báo tóm tắt
                if (result.Contains("✅ Đã thêm") && result.Contains("sản phẩm"))
                {
                    MessageBox.Show(
                        "✅ Đã tạo thành công dữ liệu mẫu!\n\n" +
                        "Bạn có thể kiểm tra trong Quản lý sản phẩm ngay bây giờ!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "⚠️ Có vấn đề khi tạo dữ liệu mẫu.\n\n" +
                        "Vui lòng kiểm tra chi tiết trong cửa sổ vừa hiển thị.",
                        "Cảnh báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Lỗi khi tạo dữ liệu mẫu:\n\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #if DEBUG
        private void TestAddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Test thêm một sản phẩm đơn giản
                string result = DatabaseHelper.TestAddProduct(
                    "Test Sản Phẩm", 
                    "TEST001", 
                    1, // CategoryId = 1 (Thực phẩm)
                    50000, 
                    10, 
                    "Sản phẩm test để kiểm tra"
                );
                
                // Tạo window hiển thị kết quả
                var resultWindow = new Window
                {
                    Title = "Kết Quả Test Thêm Sản Phẩm",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(20)
                };

                var textBlock = new TextBlock
                {
                    Text = result,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.Black
                };

                scrollViewer.Content = textBlock;
                resultWindow.Content = scrollViewer;
                resultWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Lỗi khi test thêm sản phẩm:\n\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        #endif

        private void ForceInitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn tạo lại tất cả dữ liệu mẫu?\n\n" +
                "Hành động này sẽ:\n" +
                "• Xóa tất cả dữ liệu hiện tại\n" +
                "• Tạo lại 200+ sản phẩm mẫu\n" +
                "• Tạo lại danh mục và khách hàng mẫu\n\n" +
                "Dữ liệu cũ sẽ bị mất hoàn toàn!",
                "Xác nhận tạo lại dữ liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    #if DEBUG
                    DatabaseHelper.ForceInitializeSampleData();
                    #else
                    MessageBox.Show("Chức năng này chỉ khả dụng ở bản Debug.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                    #endif
                    MessageBox.Show(
                        "✅ Đã tạo lại thành công tất cả dữ liệu mẫu!\n\n" +
                        "• 10 danh mục\n" +
                        "• 200+ sản phẩm\n" +
                        "• 5 khách hàng mẫu\n\n" +
                        "Bạn có thể kiểm tra trong Quản lý sản phẩm.",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"❌ Lỗi khi tạo lại dữ liệu:\n\n{ex.Message}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}
