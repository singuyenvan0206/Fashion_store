using System.Windows;

namespace WpfApp1
{
    public partial class CategoryManagementWindow : Window
    {
        private List<(int Id, string Name)> _categories = new();

        public CategoryManagementWindow()
        {
            InitializeComponent();
            LoadCategories();
            SetupPlaceholder();
        }

        private void SetupPlaceholder()
        {
            CategoryNameTextBox.GotFocus += (s, e) => {
                if (CategoryNameTextBox.Text == "Tên Danh Mục")
                {
                    CategoryNameTextBox.Text = "";
                    CategoryNameTextBox.Foreground = System.Windows.Media.Brushes.Black;
                }
            };
            
            CategoryNameTextBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
                {
                    CategoryNameTextBox.Text = "Tên Danh Mục";
                    CategoryNameTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };
            
            CategoryNameTextBox.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void LoadCategories()
        {
            _categories = DatabaseHelper.GetAllCategories();
            CategoryListBox.ItemsSource = null;
            CategoryListBox.ItemsSource = _categories.ConvertAll(c => new { c.Id, c.Name });
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            int count = _categories.Count;
            StatusTextBlock.Text = count == 1 ? "Tìm thấy 1 danh mục" : $"Tìm thấy {count} danh mục";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = CategoryNameTextBox.Text.Trim();
            
            if (name == "Tên Danh Mục" || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryNameTextBox.Focus();
                return;
            }
            
            if (name.Length > 255)
            {
                MessageBox.Show("Tên danh mục quá dài. Tối đa 255 ký tự được phép.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (DatabaseHelper.AddCategory(name))
            {
                LoadCategories();
                CategoryNameTextBox.Text = "Tên Danh Mục";
                CategoryNameTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                MessageBox.Show($"Danh mục '{name}' đã được thêm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Trigger dashboard refresh for real-time updates
                DashboardWindow.TriggerDashboardRefresh();
            }
            else
            {
                MessageBox.Show($"Danh mục '{name}' đã tồn tại hoặc có lỗi xảy ra.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CategoryListBox.SelectedItem;
            if (selectedItem != null)
            {
                var selected = new { Id = 0, Name = "" };
                try
                {
                    selected = (dynamic)selectedItem;
                }
                catch
                {
                    MessageBox.Show("Lựa chọn không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string name = CategoryNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Vui lòng nhập tên danh mục.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CategoryNameTextBox.Focus();
                    return;
                }
                
                if (name.Length > 255)
                {
                    MessageBox.Show("Tên danh mục quá dài. Tối đa 255 ký tự được phép.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (DatabaseHelper.UpdateCategory(selected.Id, name))
                {
                    LoadCategories();
                    CategoryNameTextBox.Clear();
                    MessageBox.Show($"Danh mục đã được cập nhật thành công thành '{name}'!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Trigger dashboard refresh for real-time updates
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else
                {
                    MessageBox.Show($"Không thể cập nhật danh mục. Tên '{name}' có thể đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn danh mục để cập nhật.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CategoryListBox.SelectedItem;
            if (selectedItem != null)
            {
                var selected = new { Id = 0, Name = "" };
                try
                {
                    selected = (dynamic)selectedItem;
                }
                catch
                {
                    MessageBox.Show("Lựa chọn không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string categoryName = selected.Name;
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa danh mục '{categoryName}'?\n\nHành động này không thể hoàn tác.",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    if (DatabaseHelper.DeleteCategory(selected.Id))
                    {
                        LoadCategories();
                        CategoryNameTextBox.Clear();
                        MessageBox.Show($"Danh mục '{categoryName}' đã được xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Trigger dashboard refresh for real-time updates
                        DashboardWindow.TriggerDashboardRefresh();
                    }
                    else
                    {
                        MessageBox.Show($"Không thể xóa danh mục '{categoryName}'. Nó có thể đang được sử dụng bởi các sản phẩm hiện có.", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn danh mục để xóa.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CategoryListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedItem = CategoryListBox.SelectedItem;
            if (selectedItem != null)
            {
                try
                {
                    var selected = (dynamic)selectedItem;
                    CategoryNameTextBox.Text = selected.Name;
                }
                catch
                {
                    CategoryNameTextBox.Text = "";
                }
            }
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa tất cả danh mục không?\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (DatabaseHelper.DeleteAllCategories())
                {
                    LoadCategories();
                    CategoryNameTextBox.Clear();
                    MessageBox.Show("Tất cả danh mục đã được xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Trigger dashboard refresh for real-time updates
                    DashboardWindow.TriggerDashboardRefresh();
                }
                else
                {
                    MessageBox.Show("Không thể xóa tất cả danh mục. Có thể có các sản phẩm đang sử dụng các danh mục này.", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}