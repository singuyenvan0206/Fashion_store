
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
            LoadPaymentSettings();
            SetupEventHandlers();
            ApplyRolePermissions();
        }
        
        private void ApplyRolePermissions()
        {
            // Get current user role from application resources
            var currentUser = Application.Current.Resources["CurrentUser"] as string;
            if (string.IsNullOrEmpty(currentUser))
                return;
                
            // Get user role from database
            var userRole = DatabaseHelper.GetUserRole(currentUser);
            var role = ParseRole(userRole);
            
            // Hide tier settings button for non-admin/manager users
            if (role == UserRole.Cashier)
            {
                if (OpenTierSettingsButton != null)
                    OpenTierSettingsButton.Visibility = Visibility.Collapsed;
            }
        }
        
        private static UserRole ParseRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return UserRole.Cashier;
            switch (role.Trim().ToLower())
            {
                case "admin": return UserRole.Admin;
                case "manager": return UserRole.Manager;
                case "cashier": return UserRole.Cashier;
                default: return UserRole.Cashier;
            }
        }

        private void LoadCurrentSettings()
        {
            var cfg = SettingsManager.Load();
            ServerTextBox.Text = cfg.Server;
            DatabaseTextBox.Text = cfg.Database;
            UserIdTextBox.Text = cfg.UserId;
            PasswordBox.Password = cfg.Password;
        }

        private void LoadPaymentSettings()
        {
            var paymentSettings = PaymentSettingsManager.Load();

            // Load QR Code toggle
            QRCodeToggleButton.IsChecked = paymentSettings.EnableQRCode;
            UpdateQRCodeStatus();

            // Load bank information
            BankAccountTextBox.Text = paymentSettings.BankAccount;
            BankCodeTextBox.Text = paymentSettings.BankCode;
            AccountHolderTextBox.Text = paymentSettings.AccountHolder;
        }

        private void SetupEventHandlers()
        {
            // No longer need PaymentMethodComboBox event handler
        }

        private void UpdateQRCodeStatus()
        {
            if (QRCodeToggleButton.IsChecked == true)
            {
                QRCodeStatusText.Text = "QR Code đã được bật";
                QRCodeStatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                QRCodeStatusText.Text = "QR Code đã được tắt";
                QRCodeStatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
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

        private void QRCodeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateQRCodeStatus();
        }

        private void QRCodeToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateQRCodeStatus();
        }

        private void SaveQRCodeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BankAccountTextBox.Text.Trim()))
                {
                    MessageBox.Show("Vui lòng nhập số tài khoản ngân hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BankAccountTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(BankCodeTextBox.Text.Trim()))
                {
                    MessageBox.Show("Vui lòng nhập mã ngân hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BankCodeTextBox.Focus();
                    return;
                }

                var paymentSettings = new PaymentSettings
                {
                    EnableQRCode = QRCodeToggleButton.IsChecked == true,
                    BankAccount = BankAccountTextBox.Text.Trim(),
                    BankCode = BankCodeTextBox.Text.Trim(),
                    BankName = "Ngân hàng",
                    AccountHolder = AccountHolderTextBox.Text.Trim()
                };

                if (PaymentSettingsManager.Save(paymentSettings))
                {
                    StatusTextBlock.Text = "Thông tin ngân hàng đã được lưu thành công!";
                    MessageBox.Show("Thông tin ngân hàng đã được lưu thành công!", "Đã lưu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể lưu thông tin ngân hàng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu thông tin ngân hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenTierSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tierSettingsWindow = new TierSettingsWindow();

                try
                {
                    tierSettingsWindow.Owner = this;
                    tierSettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                catch
                {
                    tierSettingsWindow.Owner = null;
                    tierSettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                tierSettingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cài đặt hạng thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}