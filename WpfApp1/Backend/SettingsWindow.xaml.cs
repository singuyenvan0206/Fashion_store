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
            LoadPaymentSettings();
            SetupEventHandlers();
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
            
            // Load payment method
            foreach (ComboBoxItem item in PaymentMethodComboBox.Items)
            {
                if (item.Tag?.ToString() == paymentSettings.PaymentMethod)
                {
                    PaymentMethodComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Load payment info based on method
            UpdatePaymentInfoFields(paymentSettings.PaymentMethod);
            PaymentInfoTextBox.Text = paymentSettings.PaymentMethod.ToLower() switch
            {
                "momo" => paymentSettings.MoMoPhone,
                "tpbank" => paymentSettings.BankAccount,
                "bank" => paymentSettings.BankAccount,
                _ => paymentSettings.MoMoPhone
            };
            
            BankAccountTextBox.Text = paymentSettings.BankAccount;
            BankNameTextBox.Text = paymentSettings.BankName;
            BankBINTextBox.Text = paymentSettings.BankBIN;
        }

        private void SetupEventHandlers()
        {
            PaymentMethodComboBox.SelectionChanged += PaymentMethodComboBox_SelectionChanged;
        }

        private void UpdateQRCodeStatus()
        {
            if (QRCodeToggleButton.IsChecked == true)
            {
                QRCodeStatusText.Text = "QR Code đã được bật";
                QRCodeStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            else
            {
                QRCodeStatusText.Text = "QR Code đã được tắt";
                QRCodeStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private void UpdatePaymentInfoFields(string paymentMethod)
        {
            switch (paymentMethod.ToLower())
            {
                case "momo":
                    PaymentInfoLabel.Text = "Số điện thoại MoMo:";
                    PaymentInfoLabel.Visibility = Visibility.Visible;
                    PaymentInfoTextBox.Visibility = Visibility.Visible;
                    BankAccountLabel.Visibility = Visibility.Collapsed;
                    BankAccountTextBox.Visibility = Visibility.Collapsed;
                    BankNameLabel.Visibility = Visibility.Collapsed;
                    BankNameTextBox.Visibility = Visibility.Collapsed;
                    BankBINLabel.Visibility = Visibility.Collapsed;
                    BankBINTextBox.Visibility = Visibility.Collapsed;
                    break;
                case "tpbank":
                    PaymentInfoLabel.Text = "Số tài khoản TPBank:";
                    PaymentInfoLabel.Visibility = Visibility.Visible;
                    PaymentInfoTextBox.Visibility = Visibility.Visible;
                    BankAccountLabel.Visibility = Visibility.Collapsed;
                    BankAccountTextBox.Visibility = Visibility.Collapsed;
                    BankNameLabel.Visibility = Visibility.Collapsed;
                    BankNameTextBox.Visibility = Visibility.Collapsed;
                    BankBINLabel.Visibility = Visibility.Collapsed;
                    BankBINTextBox.Visibility = Visibility.Collapsed;
                    break;
                case "bank":
                    PaymentInfoLabel.Visibility = Visibility.Collapsed;
                    PaymentInfoTextBox.Visibility = Visibility.Collapsed;
                    BankAccountLabel.Visibility = Visibility.Visible;
                    BankAccountTextBox.Visibility = Visibility.Visible;
                    BankNameLabel.Visibility = Visibility.Visible;
                    BankNameTextBox.Visibility = Visibility.Visible;
                    BankBINLabel.Visibility = Visibility.Visible;
                    BankBINTextBox.Visibility = Visibility.Visible;
                    break;
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

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string paymentMethod = selectedItem.Tag?.ToString() ?? "momo";
                UpdatePaymentInfoFields(paymentMethod);
            }
        }

        private void SaveQRCodeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var paymentSettings = new PaymentSettings
                {
                    EnableQRCode = QRCodeToggleButton.IsChecked == true,
                    PaymentMethod = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "momo"
                };

                // Update payment info based on selected method
                switch (paymentSettings.PaymentMethod.ToLower())
                {
                    case "momo":
                        paymentSettings.MoMoPhone = PaymentInfoTextBox.Text.Trim();
                        break;
                    case "tpbank":
                        paymentSettings.BankAccount = PaymentInfoTextBox.Text.Trim();
                        paymentSettings.BankBIN = "970423"; // TPBank BIN
                        paymentSettings.BankName = "TPBank";
                        break;
                    case "bank":
                        paymentSettings.BankAccount = BankAccountTextBox.Text.Trim();
                        paymentSettings.BankName = BankNameTextBox.Text.Trim();
                        paymentSettings.BankBIN = BankBINTextBox.Text.Trim();
                        break;
                }

                if (PaymentSettingsManager.Save(paymentSettings))
                {
                    StatusTextBlock.Text = "Cài đặt QR Code đã được lưu thành công!";
                    MessageBox.Show("Cài đặt QR Code đã được lưu thành công!", "Đã lưu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể lưu cài đặt QR Code. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt QR Code: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}