using System.Windows;

namespace WpfApp1
{
    public partial class PasswordMigrationWindow : Window
    {
        public PasswordMigrationWindow()
        {
            InitializeComponent();
        }

        private void RunMigrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunMigrationButton.IsEnabled = false;
                StatusBorder.Visibility = Visibility.Visible;
                StatusText.Text = "Đang chạy migration... Vui lòng đợi.";

                // Chạy migration
                var (success, migratedCount, message) = DatabaseHelper.RunPasswordMigration();

                if (success)
                {
                    StatusBorder.Background = migratedCount > 0 
                        ? System.Windows.Media.Brushes.LightGreen 
                        : System.Windows.Media.Brushes.LightBlue;
                    StatusBorder.BorderBrush = migratedCount > 0 
                        ? System.Windows.Media.Brushes.Green 
                        : System.Windows.Media.Brushes.Blue;
                    StatusText.Foreground = migratedCount > 0 
                        ? System.Windows.Media.Brushes.DarkGreen 
                        : System.Windows.Media.Brushes.DarkBlue;
                    StatusText.Text = $"✅ {message}";
                }
                else
                {
                    StatusBorder.Background = System.Windows.Media.Brushes.LightPink;
                    StatusBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                    StatusText.Foreground = System.Windows.Media.Brushes.DarkRed;
                    StatusText.Text = $"❌ {message}";
                }
            }
            catch (System.Exception ex)
            {
                StatusBorder.Background = System.Windows.Media.Brushes.LightPink;
                StatusBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                StatusText.Foreground = System.Windows.Media.Brushes.DarkRed;
                StatusText.Text = $"❌ Lỗi: {ex.Message}";
            }
            finally
            {
                RunMigrationButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

