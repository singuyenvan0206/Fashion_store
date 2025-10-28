using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameTextBox?.Focus();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(LoginButton, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string role = DatabaseHelper.ValidateLogin(username, password);
            if (role == "true")
            {
                // Get the actual role from database
                var userRole = DatabaseHelper.GetUserRoleEnum(username);

                // Use a single DashboardWindow for all roles; features are hidden based on role inside the dashboard
                Window dashboard = new DashboardWindow(username, userRole.ToString());

                // Set the dashboard as the new main window
                Application.Current.MainWindow = dashboard;
                dashboard.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var changePasswordWindow = new ChangePasswordWindow();
            try
            {
                if (this.IsLoaded)
                {
                    changePasswordWindow.Owner = this;
                    changePasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    changePasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            catch (InvalidOperationException)
            {
                changePasswordWindow.Owner = null;
                changePasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            changePasswordWindow.ShowDialog();
        }
    }
}