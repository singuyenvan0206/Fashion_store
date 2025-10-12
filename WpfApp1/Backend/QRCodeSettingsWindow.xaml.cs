using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class QRCodeSettingsWindow : Window
    {
        private PaymentSettings _currentSettings = new PaymentSettings();

        public QRCodeSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            SetupEventHandlers();
            UpdatePreview();
        }

        private void LoadSettings()
        {
            _currentSettings = PaymentSettingsManager.Load();
            
            // Load settings into UI
            EnableQRCodeCheckBox.IsChecked = _currentSettings.EnableQRCode;
            
            // Set payment method
            foreach (ComboBoxItem item in PaymentMethodComboBox.Items)
            {
                if (item.Tag?.ToString() == _currentSettings.PaymentMethod)
                {
                    PaymentMethodComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Load payment info
            MoMoPhoneTextBox.Text = _currentSettings.MoMoPhone;
            ZaloPayPhoneTextBox.Text = _currentSettings.ZaloPayPhone;
            BankAccountTextBox.Text = _currentSettings.BankAccount;
            BankNameTextBox.Text = _currentSettings.BankName;
            AccountNameTextBox.Text = _currentSettings.AccountName;
        }

        private void SetupEventHandlers()
        {
            PaymentMethodComboBox.SelectionChanged += PaymentMethodComboBox_SelectionChanged;
            EnableQRCodeCheckBox.Checked += EnableQRCodeCheckBox_Changed;
            EnableQRCodeCheckBox.Unchecked += EnableQRCodeCheckBox_Changed;
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePaymentMethodVisibility();
            UpdatePreview();
        }

        private void EnableQRCodeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePaymentMethodVisibility()
        {
            var selectedItem = PaymentMethodComboBox.SelectedItem as ComboBoxItem;
            var method = selectedItem?.Tag?.ToString() ?? "momo";

            // Hide all payment method settings
            MoMoSettingsBorder.Visibility = Visibility.Collapsed;
            ZaloPaySettingsBorder.Visibility = Visibility.Collapsed;
            BankSettingsBorder.Visibility = Visibility.Collapsed;

            // Show selected payment method settings
            switch (method)
            {
                case "momo":
                    MoMoSettingsBorder.Visibility = Visibility.Visible;
                    break;
                case "zalopay":
                    ZaloPaySettingsBorder.Visibility = Visibility.Visible;
                    break;
                case "bank":
                    BankSettingsBorder.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdatePreview()
        {
            try
            {
                if (PreviewQRCode == null || EnableQRCodeCheckBox?.IsChecked != true)
                {
                    if (PreviewQRCode != null) PreviewQRCode.Source = null;
                    return;
                }

                var selectedItem = PaymentMethodComboBox.SelectedItem as ComboBoxItem;
                var method = selectedItem?.Tag?.ToString() ?? "momo";
                
                decimal amount = decimal.TryParse(PreviewAmountTextBox.Text, out decimal parsedAmount) ? parsedAmount : 500000;

                string paymentInfo = method switch
                {
                    "momo" => MoMoPhoneTextBox.Text,
                    "zalopay" => ZaloPayPhoneTextBox.Text,
                    "bank" => BankAccountTextBox.Text,
                    _ => MoMoPhoneTextBox.Text
                };

                var qrCode = QRCodeHelper.GenerateInvoicePaymentQR(999999, amount, method, paymentInfo);
                PreviewQRCode.Source = qrCode;
            }
            catch
            {
                // Ignore preview errors
            }
        }

        private void PreviewAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update settings from UI
                _currentSettings.EnableQRCode = EnableQRCodeCheckBox.IsChecked == true;
                
                var selectedItem = PaymentMethodComboBox.SelectedItem as ComboBoxItem;
                _currentSettings.PaymentMethod = selectedItem?.Tag?.ToString() ?? "momo";
                
                _currentSettings.MoMoPhone = MoMoPhoneTextBox.Text;
                _currentSettings.ZaloPayPhone = ZaloPayPhoneTextBox.Text;
                _currentSettings.BankAccount = BankAccountTextBox.Text;
                _currentSettings.BankName = BankNameTextBox.Text;
                _currentSettings.AccountName = AccountNameTextBox.Text;

                // Save settings
                if (PaymentSettingsManager.Save(_currentSettings))
                {
                    MessageBox.Show("✅ Cài đặt QR code đã được lưu thành công!", "Thành công", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Không thể lưu cài đặt. Vui lòng thử lại!", "Lỗi", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
