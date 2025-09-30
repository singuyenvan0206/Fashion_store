using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class UserManagementWindow : Window
    {
        private List<UserManagementItem> _allUsers = new();
        private List<UserManagementItem> _filteredUsers = new();

        public UserManagementWindow()
        {
            InitializeComponent();
            LoadUsers();
            RoleFilterComboBox.SelectedIndex = 0; // Select "All roles"
        }

        private void LoadUsers()
        {
            try
            {
                // Use GetAllAccounts and map to UserManagementItem
                var accounts = DatabaseHelper.GetAllAccounts();
                _allUsers = accounts.Select(a => new UserManagementItem
                {
                    Id = a.Id,
                    Username = a.Username,
                    EmployeeName = "", // No EmployeeName available from GetAllAccounts
                    Role = DatabaseHelper.GetUserRoleEnum(a.Username)
                }).ToList();

                FilterUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterUsers()
        {
            try
            {
                string searchText = SearchTextBox.Text?.ToLower() ?? "";
                string roleFilter = (RoleFilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

                _filteredUsers = _allUsers.Where(u => 
                    (string.IsNullOrEmpty(searchText) ||
                        u.Username.ToLower().Contains(searchText) ||
                        (u.EmployeeName ?? string.Empty).ToLower().Contains(searchText)) &&
                    (string.IsNullOrEmpty(roleFilter) || u.Role.ToString() == roleFilter)
                ).ToList();

                UsersDataGrid.ItemsSource = _filteredUsers;
                StatusTextBlock.Text = $"Tìm thấy {_filteredUsers.Count} người dùng";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lọc danh sách: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is UserManagementItem user)
            {
                var editUserWindow = new AddEditUserWindow(user);
                if (editUserWindow.ShowDialog() == true)
                {
                    LoadUsers();
                    MessageBox.Show("Cập nhật người dùng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterUsers();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            FilterUsers();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addUserWindow = new AddEditUserWindow();
                if (addUserWindow.ShowDialog() == true)
                {
                    LoadUsers();
                    MessageBox.Show("Thêm người dùng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is UserManagementItem user)
                {
                    var editUserWindow = new AddEditUserWindow(user);
                    if (editUserWindow.ShowDialog() == true)
                    {
                        LoadUsers();
                        MessageBox.Show("Cập nhật người dùng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is UserManagementItem user)
                {
                    if (user.Username == "admin")
                    {
                        MessageBox.Show("Không thể xóa tài khoản admin!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa người dùng '{user.Username}'?", 
                        "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        if (DatabaseHelper.DeleteAccount(user.Username))
                        {
                            LoadUsers();
                            MessageBox.Show("Xóa người dùng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Lỗi xóa người dùng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class UserManagementItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string RoleDisplayName => Role.GetDisplayName();
        public string RoleDescription => Role.GetDescription();
    }
}
