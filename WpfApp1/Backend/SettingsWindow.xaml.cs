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
    }
}