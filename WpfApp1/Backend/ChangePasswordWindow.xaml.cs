using System.Windows;

namespace WpfApp1
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Vui lòng điền đầy đủ tất cả các trường.");
                return;
            }

            bool success = DatabaseHelper.ChangePassword(username, oldPassword, newPassword);
            if (success)
            {
                MessageBox.Show("Mật khẩu đã được thay đổi thành công.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu cũ không đúng.");
            }
        }
    }
}