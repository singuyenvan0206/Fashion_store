
using System.Windows;

namespace WpfApp1
{

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TryInitializeDatabaseWithFallback();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                foreach (Window w in Current.Windows)
                {
                    try { w.Close(); } catch { }
                }
            }
            catch { }

            try
            {
                MySql.Data.MySqlClient.MySqlConnection.ClearAllPools();
            }
            catch { }
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch { }

            base.OnExit(e);

            // Hard-exit the process to ensure no background tasks keep it alive
            try 
            { 
                System.Environment.Exit(0); 
            } 
            catch 
            { 
                // Last resort - force terminate
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        private void TryInitializeDatabaseWithFallback()
        {
            try
            {
                DatabaseHelper.InitializeDatabase();
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
