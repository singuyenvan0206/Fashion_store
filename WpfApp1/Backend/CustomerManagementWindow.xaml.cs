using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CustomerManagementWindow : Window
    {
        private List<CustomerViewModel> _customers = new();
        private CustomerViewModel? _selectedCustomer;
        private PaginationHelper<CustomerViewModel> _paginationHelper = new();

        public CustomerManagementWindow()
        {
            InitializeComponent();
            _selectedCustomer = new CustomerViewModel();
            _paginationHelper.PageChanged += OnPageChanged;
            LoadCustomers();
            
            // Enable sorting for DataGrid
            CustomerDataGrid.Sorting += CustomerDataGrid_Sorting;
            
            // Apply role-based permissions
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
                    
                // Disable tier and points editing for cashiers
                if (TierComboBox != null)
                {
                    TierComboBox.IsEnabled = false;
                    TierComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                    TierComboBox.ToolTip = "Chỉ Admin và Manager mới có thể thay đổi hạng thành viên";
                }
                if (PointsTextBox != null)
                {
                    PointsTextBox.IsReadOnly = true;
                    PointsTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                    PointsTextBox.ToolTip = "Chỉ Admin và Manager mới có thể thay đổi điểm tích lũy";
                }
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

        private void LoadCustomers()
        {
            var customers = DatabaseHelper.GetAllCustomers();
            _customers = customers.ConvertAll(c => new CustomerViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                CustomerType = c.CustomerType,
                Tier = DatabaseHelper.GetCustomerLoyalty(c.Id).Tier,
                Points = DatabaseHelper.GetCustomerLoyalty(c.Id).Points
            });
            _paginationHelper.SetData(_customers);
            UpdateDisplayAndPagination();
        }
        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Chọn tệp CSV để nhập khách hàng"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                int importedCount = DatabaseHelper.ImportCustomersFromCsv(filePath);
                if (importedCount >= 0)
                {
                    LoadCustomers();
                    MessageBox.Show($"Đã nhập thành công {importedCount} khách hàng từ tệp CSV.", "Nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Trigger dashboard refresh for real-time updates
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else
                {
                    MessageBox.Show("Không thể nhập khách hàng từ tệp CSV. Vui lòng kiểm tra định dạng tệp.", "Lỗi nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Lưu khách hàng vào tệp CSV",
                FileName = "customers.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                bool success = DatabaseHelper.ExportCustomersToCsv(filePath);
                if (success)
                {
                    MessageBox.Show("Đã xuất khách hàng thành công sang tệp CSV.", "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể xuất khách hàng sang tệp CSV. Vui lòng thử lại.", "Lỗi xuất", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void UpdateStatusText()
        {
            int count = _paginationHelper.TotalItems;
            StatusTextBlock.Text = count == 1 ? "Tìm thấy 1 khách hàng" : $"Tìm thấy {count} khách hàng";
        }

        private void OnPageChanged()
        {
            UpdateDisplayAndPagination();
        }

        private void UpdateDisplayAndPagination()
        {
            // Update DataGrid with current page items
            CustomerDataGrid.ItemsSource = _paginationHelper.GetCurrentPageItems();
            
            // Update pagination info
            if (CustomerPageInfoTextBlock != null)
            {
                CustomerPageInfoTextBlock.Text = $"📄 Trang: {_paginationHelper.GetPageInfo()} • 👥 Tổng: {_paginationHelper.TotalItems} khách hàng";
            }
            
            // Update current page textbox
            if (CustomerCurrentPageTextBox != null)
            {
                CustomerCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
            }
            
            // Update button states
            if (CustomerFirstPageButton != null) CustomerFirstPageButton.IsEnabled = _paginationHelper.CanGoFirst;
            if (CustomerPrevPageButton != null) CustomerPrevPageButton.IsEnabled = _paginationHelper.CanGoPrevious;
            if (CustomerNextPageButton != null) CustomerNextPageButton.IsEnabled = _paginationHelper.CanGoNext;
            if (CustomerLastPageButton != null) CustomerLastPageButton.IsEnabled = _paginationHelper.CanGoLast;
            
            UpdateStatusText();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            var customer = new CustomerViewModel
            {
                Name = CustomerNameTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                CustomerType = "Regular",
                Address = AddressTextBox.Text.Trim()
            };

            if (DatabaseHelper.AddCustomer(customer.Name, customer.Phone, customer.Email, customer.CustomerType, customer.Address))
            {
                try
                {
                    int id = DatabaseHelper.GetAllCustomers().Last().Id;

                    // Check if user has permission to set tier and points
                    var currentUser = Application.Current.Resources["CurrentUser"] as string;
                    var userRole = DatabaseHelper.GetUserRole(currentUser ?? "");
                    var role = ParseRole(userRole);

                    if (role.CanManageTierSettings())
                    {
                        var tier = (TierComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Regular";
                        int pts = 0; int.TryParse(PointsTextBox.Text, out pts);
                        DatabaseHelper.UpdateCustomerLoyalty(id, pts, tier);
                    }
                    else
                    {
                        // For cashiers, always set to Regular with 0 points
                        DatabaseHelper.UpdateCustomerLoyalty(id, 0, "Regular");
                    }
                } catch {}
                LoadCustomers();
                ClearForm();
                MessageBox.Show($"Khách hàng '{customer.Name}' đã được thêm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Trigger dashboard refresh for real-time updates
                DashboardWindow.TriggerDashboardRefresh();
            }
            else
            {
                MessageBox.Show("Không thể thêm khách hàng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng để cập nhật.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateInput()) return;

            var customer = new CustomerViewModel
            {
                Id = _selectedCustomer.Id,
                Name = CustomerNameTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                CustomerType = _selectedCustomer.CustomerType ?? "Regular",
                Address = AddressTextBox.Text.Trim()
            };

            if (DatabaseHelper.UpdateCustomer(customer.Id, customer.Name, customer.Phone, customer.Email, customer.CustomerType, customer.Address))
            {
                // Update loyalty only if user has permission
                try
                {
                    var currentUser = Application.Current.Resources["CurrentUser"] as string;
                    var userRole = DatabaseHelper.GetUserRole(currentUser ?? "");
                    var role = ParseRole(userRole);
                    
                    if (role.CanManageTierSettings())
                    {
                        var tier = (TierComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _selectedCustomer.Tier;
                        int pts = _selectedCustomer.Points; int.TryParse(PointsTextBox.Text, out pts);
                        DatabaseHelper.UpdateCustomerLoyalty(customer.Id, pts, tier);
                    }
                    // If cashier, keep existing tier and points unchanged
                } catch {}
                LoadCustomers();
                ClearForm();
                MessageBox.Show($"Khách hàng '{customer.Name}' đã được cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Trigger dashboard refresh for real-time updates
                DashboardWindow.TriggerDashboardRefresh();
            }
            else
            {
                MessageBox.Show("Không thể cập nhật khách hàng. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng để xóa.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string customerName = _selectedCustomer.Name;
            int customerId = _selectedCustomer.Id;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa khách hàng '{customerName}'?\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (DatabaseHelper.DeleteCustomer(customerId))
                {
                    LoadCustomers();
                    ClearForm();
                    MessageBox.Show($"Khách hàng '{customerName}' đã được xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Trigger dashboard refresh for real-time updates
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else
                {
                    MessageBox.Show("Không thể xóa khách hàng. Khách hàng có thể đang được sử dụng trong hóa đơn.", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void DeleteAllCustomersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_customers.Count == 0)
            {
                MessageBox.Show("Không có khách hàng nào để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa TẤT CẢ {_customers.Count} khách hàng?\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa tất cả",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (DatabaseHelper.DeleteAllCustomers())
                {
                    LoadCustomers();
                    ClearForm();
                    MessageBox.Show("Đã xóa tất cả khách hàng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Trigger dashboard refresh for real-time updates
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else
                {
                    MessageBox.Show("Không thể xóa tất cả khách hàng. Vui lòng kiểm tra ràng buộc dữ liệu (hóa đơn liên quan).", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearForm()
        {
            CustomerNameTextBox.Clear();
            PhoneTextBox.Clear();
            EmailTextBox.Clear();
            // CustomerType removed: default Regular
            AddressTextBox.Clear();
            TierComboBox.SelectedIndex = 0;
            PointsTextBox.Text = "0";
            _selectedCustomer = null;
            CustomerDataGrid.SelectedItem = null;
            CustomerNameTextBox.Focus();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên khách hàng.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerNameTextBox.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) && !IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ email hợp lệ.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void CustomerDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCustomer = CustomerDataGrid.SelectedItem as CustomerViewModel;
            if (_selectedCustomer != null)
            {
                CustomerNameTextBox.Text = _selectedCustomer.Name ?? "";
                PhoneTextBox.Text = _selectedCustomer.Phone ?? "";
                EmailTextBox.Text = _selectedCustomer.Email ?? "";
                AddressTextBox.Text = _selectedCustomer.Address ?? "";
                
                // Set the customer type in combo box
                // CustomerType removed

                // Segment removed

                // Set loyalty tier and points
                try
                {
                    var (tier, pts) = DatabaseHelper.GetCustomerLoyalty(_selectedCustomer.Id);
                    foreach (ComboBoxItem item in TierComboBox.Items)
                    {
                        if (string.Equals(item.Content?.ToString(), tier, System.StringComparison.OrdinalIgnoreCase))
                        {
                            TierComboBox.SelectedItem = item;
                            break;
                        }
                    }
                    PointsTextBox.Text = pts.ToString();
                }
                catch {}

                // Load purchase history for the selected customer
                LoadPurchaseHistory(_selectedCustomer.Id);
            }
        }

        private void LoadPurchaseHistory(int customerId)
        {
            try
            {
                var history = DatabaseHelper.GetCustomerPurchaseHistory(customerId)
                    .Select(h => new PurchaseHistoryItem
                    {
                        InvoiceId = h.InvoiceId,
                        CreatedAt = h.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        ItemCount = h.ItemCount,
                        Total = h.Total.ToString("N0")
                    })
                    .ToList();
                PurchaseHistoryDataGrid.ItemsSource = history;
            }
            catch
            {
                PurchaseHistoryDataGrid.ItemsSource = null;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCustomers();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterCustomers();
        }

        private void FilterCustomers()
        {
            string searchTerm = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _paginationHelper.SetFilter(null!);
            }
            else
            {
                _paginationHelper.SetFilter(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    c.Phone.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.CustomerType.ToLower().Contains(searchTerm) ||
                    c.Address.ToLower().Contains(searchTerm) ||
                    (c.Tier ?? string.Empty).ToLower().Contains(searchTerm) ||
                    c.Points.ToString().Contains(searchTerm)
                );
            }
        }
        
        private void UpdateLoyaltyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng.");
                return;
            }
            var tier = (TierComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _selectedCustomer.Tier;
            int pts = _selectedCustomer.Points; int.TryParse(PointsTextBox.Text, out pts);
            if (DatabaseHelper.UpdateCustomerLoyalty(_selectedCustomer.Id, pts, tier))
            {
                LoadCustomers();
                MessageBox.Show("Đã cập nhật hạng/điểm.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Trigger dashboard refresh for real-time updates
                DashboardWindow.TriggerDashboardRefresh();
            }
            else
            {
                MessageBox.Show("Cập nhật thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TierSettingsButton_Click(object sender, RoutedEventArgs e)
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

            // Refresh customer list after tier settings might have changed
            LoadCustomers();

            // Trigger dashboard refresh for real-time updates
            DashboardWindow.TriggerDashboardRefresh();
        }


        // Pagination event handlers
        private void CustomerFirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.FirstPage();
        }

        private void CustomerPrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.PreviousPage();
        }

        private void CustomerNextPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.NextPage();
        }

        private void CustomerLastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.LastPage();
        }

        private void CustomerCurrentPageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (int.TryParse(CustomerCurrentPageTextBox.Text, out int pageNumber))
                {
                    if (!_paginationHelper.GoToPage(pageNumber))
                    {
                        // Reset to current page if invalid
                        CustomerCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                        MessageBox.Show($"Trang không hợp lệ. Vui lòng nhập từ 1 đến {_paginationHelper.TotalPages}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    CustomerCurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                }
            }
        }
        
        private void CustomerDataGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
        {
            e.Handled = true; // Prevent default sorting
            
            var column = e.Column;
            var propertyName = column.SortMemberPath;
            
            if (string.IsNullOrEmpty(propertyName)) return;
            
            // Determine sort direction
            var direction = column.SortDirection != System.ComponentModel.ListSortDirection.Ascending 
                ? System.ComponentModel.ListSortDirection.Ascending 
                : System.ComponentModel.ListSortDirection.Descending;
            
            // Apply sort to all data through PaginationHelper
            Func<IEnumerable<CustomerViewModel>, IOrderedEnumerable<CustomerViewModel>>? sortFunc = null;
            
            switch (propertyName.ToLower())
            {
                case "id":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Id)
                        : items => items.OrderByDescending(c => c.Id);
                    break;
                case "name":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Name)
                        : items => items.OrderByDescending(c => c.Name);
                    break;
                case "phone":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Phone)
                        : items => items.OrderByDescending(c => c.Phone);
                    break;
                case "email":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Email)
                        : items => items.OrderByDescending(c => c.Email);
                    break;
                case "address":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Address)
                        : items => items.OrderByDescending(c => c.Address);
                    break;
                case "tier":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Tier)
                        : items => items.OrderByDescending(c => c.Tier);
                    break;
                case "points":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(c => c.Points)
                        : items => items.OrderByDescending(c => c.Points);
                    break;
            }
            
            if (sortFunc != null)
            {
                _paginationHelper.SetSort(sortFunc);
                
                // Update column sort direction
                column.SortDirection = direction;
                
                // Clear other columns' sort direction
                foreach (var col in CustomerDataGrid.Columns)
                {
                    if (col != column)
                        col.SortDirection = null;
                }
            }
        }
    }

    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string CustomerType { get; set; } = "Regular";
        public string Tier { get; set; } = "Regular";
        public int Points { get; set; }
    }

    public class PurchaseHistoryItem
    {
        public int InvoiceId { get; set; }
        public string CreatedAt { get; set; } = "";
        public int ItemCount { get; set; }
        public string Total { get; set; } = "";
    }
}
