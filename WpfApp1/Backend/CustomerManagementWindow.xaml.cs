using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CustomerManagementWindow : Window
    {
        private List<CustomerViewModel> _customers = new();
        private CustomerViewModel _selectedCustomer;

        public CustomerManagementWindow()
        {
            InitializeComponent();
            _selectedCustomer = new CustomerViewModel();
            LoadCustomers();
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
                CustomerType = c.CustomerType
            });
            CustomerDataGrid.ItemsSource = _customers;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            int count = _customers.Count;
            StatusTextBlock.Text = count == 1 ? "Tìm thấy 1 khách hàng" : $"Tìm thấy {count} khách hàng";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            var customer = new CustomerViewModel
            {
                Name = CustomerNameTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                CustomerType = (CustomerTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Regular",
                Address = AddressTextBox.Text.Trim()
            };

            if (DatabaseHelper.AddCustomer(customer.Name, customer.Phone, customer.Email, customer.CustomerType, customer.Address))
            {
                // Update segment if provided
                var segment = (SegmentComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                try { DatabaseHelper.UpdateCustomerSegment(DatabaseHelper.GetAllCustomers().Last().Id, segment ?? ""); } catch {}
                LoadCustomers();
                ClearForm();
                MessageBox.Show($"Khách hàng '{customer.Name}' đã được thêm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
                CustomerType = (CustomerTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Regular",
                Address = AddressTextBox.Text.Trim()
            };

            if (DatabaseHelper.UpdateCustomer(customer.Id, customer.Name, customer.Phone, customer.Email, customer.CustomerType, customer.Address))
            {
                // Update segment
                var segment = (SegmentComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                try { DatabaseHelper.UpdateCustomerSegment(customer.Id, segment ?? ""); } catch {}
                LoadCustomers();
                ClearForm();
                MessageBox.Show($"Khách hàng '{customer.Name}' đã được cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ClearForm()
        {
            CustomerNameTextBox.Clear();
            PhoneTextBox.Clear();
            EmailTextBox.Clear();
            CustomerTypeComboBox.SelectedIndex = 0;
            AddressTextBox.Clear();
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
                foreach (ComboBoxItem item in CustomerTypeComboBox.Items)
                {
                    if (item.Content.ToString() == (_selectedCustomer.CustomerType ?? ""))
                    {
                        CustomerTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Set segment from database if available
                try
                {
                    var seg = DatabaseHelper.GetCustomerSegment(_selectedCustomer.Id);
                    if (!string.IsNullOrEmpty(seg))
                    {
                        foreach (ComboBoxItem item in SegmentComboBox.Items)
                        {
                            if (string.Equals(item.Content?.ToString(), seg, System.StringComparison.OrdinalIgnoreCase))
                            {
                                SegmentComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
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
            var filteredCustomers = _customers.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                c.Phone.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.CustomerType.ToLower().Contains(searchTerm) ||
                (DatabaseHelper.GetCustomerSegment(c.Id) ?? string.Empty).ToLower().Contains(searchTerm) ||
                c.Address.ToLower().Contains(searchTerm)
            ).ToList();
            CustomerDataGrid.ItemsSource = filteredCustomers;
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
    }

    public class PurchaseHistoryItem
    {
        public int InvoiceId { get; set; }
        public string CreatedAt { get; set; } = "";
        public int ItemCount { get; set; }
        public string Total { get; set; } = "";
    }
}
