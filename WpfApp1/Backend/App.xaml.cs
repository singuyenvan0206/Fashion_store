using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TryInitializeDatabaseWithFallback();
        }

        private void TryInitializeDatabaseWithFallback()
        {
            try
            {
                DatabaseHelper.InitializeDatabase();
                DatabaseHelper.InitializeDefaultVietnameseData();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Khởi tạo cơ sở dữ liệu thất bại.\n\n{ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                var result = MessageBox.Show("Mở cài đặt để cấu hình kết nối cơ sở dữ liệu ngay bây giờ?", "Kết nối cơ sở dữ liệu", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var settings = new SettingsWindow();
                    settings.ShowDialog();
                    try
                    {
                        DatabaseHelper.InitializeDatabase();
                        DatabaseHelper.InitializeDefaultVietnameseData();
                    }
                    catch (System.Exception retryEx)
                    {
                        MessageBox.Show($"Cơ sở dữ liệu vẫn không khả dụng. Ứng dụng có thể không hoạt động đúng.\n\n{retryEx.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

}
