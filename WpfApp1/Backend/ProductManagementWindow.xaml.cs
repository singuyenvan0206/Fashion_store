using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class ProductManagementWindow : Window
    {
        private List<ProductViewModel> _products = new();
        private List<CategoryViewModel> _categories = new();
        private ProductViewModel? _selectedProduct;
        private PaginationHelper<ProductViewModel> _paginationHelper = new();

        public ProductManagementWindow()
        {
            InitializeComponent();
            _selectedProduct = new ProductViewModel();
            _paginationHelper.PageChanged += OnPageChanged;
            LoadData();
            
            // Enable sorting for DataGrid
            ProductDataGrid.Sorting += ProductDataGrid_Sorting;
        }

        private void LoadData()
        {
            LoadCategories();
            LoadProducts();
        }

        private void LoadCategories()
        {
            var categories = DatabaseHelper.GetAllCategories();
            _categories = categories.ConvertAll(c => new CategoryViewModel { Id = c.Id, Name = c.Name });
            CategoryComboBox.ItemsSource = _categories;
        }

        private void LoadProducts()
        {
            var products = DatabaseHelper.GetAllProductsWithCategories();
            _products = products.ConvertAll(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryName,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Description = p.Description
            });
            _paginationHelper.SetData(_products);
            UpdateDisplayAndPagination();
        }

        private void UpdateStatusText()
        {
            int count = _paginationHelper.TotalItems;
            StatusTextBlock.Text = count == 1 ? "Tìm thấy 1 sản phẩm" : $"Tìm thấy {count} sản phẩm";
        }

        private void OnPageChanged()
        {
            UpdateDisplayAndPagination();
        }

        private void UpdateDisplayAndPagination()
        {
            // Update DataGrid with current page items
            ProductDataGrid.ItemsSource = _paginationHelper.GetCurrentPageItems();
            
            // Update pagination info
            if (PageInfoTextBlock != null)
            {
                PageInfoTextBlock.Text = $"📄 Trang: {_paginationHelper.GetPageInfo()} • 📊 Tổng: {_paginationHelper.TotalItems} sản phẩm";
            }
            
            // Update current page textbox
            if (CurrentPageTextBox != null)
            {
                CurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
            }
            
            // Update button states
            if (FirstPageButton != null) FirstPageButton.IsEnabled = _paginationHelper.CanGoFirst;
            if (PrevPageButton != null) PrevPageButton.IsEnabled = _paginationHelper.CanGoPrevious;
            if (NextPageButton != null) NextPageButton.IsEnabled = _paginationHelper.CanGoNext;
            if (LastPageButton != null) LastPageButton.IsEnabled = _paginationHelper.CanGoLast;
            
            UpdateStatusText();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            var product = new ProductViewModel
            {
                Name = ProductNameTextBox.Text.Trim(),
                Code = ProductCodeTextBox.Text.Trim(),
                CategoryId = CategoryComboBox.SelectedValue as int? ?? 0,
                Price = decimal.Parse(PriceTextBox.Text),
                StockQuantity = int.Parse(StockQuantityTextBox.Text),
                Description = DescriptionTextBox.Text.Trim()
            };

            if (DatabaseHelper.AddProduct(product.Name, product.Code, product.CategoryId, product.Price, product.StockQuantity, product.Description))
            {
                LoadProducts();
                ClearForm();
                MessageBox.Show($"Sản phẩm '{product.Name}' đã được thêm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Không thể thêm sản phẩm. Mã sản phẩm có thể đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm để cập nhật.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateInput()) return;

            var product = new ProductViewModel
            {
                Id = _selectedProduct.Id,
                Name = ProductNameTextBox.Text.Trim(),
                Code = ProductCodeTextBox.Text.Trim(),
                CategoryId = CategoryComboBox.SelectedValue as int? ?? 0,
                Price = decimal.Parse(PriceTextBox.Text),
                StockQuantity = int.Parse(StockQuantityTextBox.Text),
                Description = DescriptionTextBox.Text.Trim()
            };

            if (DatabaseHelper.UpdateProduct(product.Id, product.Name, product.Code, product.CategoryId, product.Price, product.StockQuantity, product.Description))
            {
                LoadProducts();
                ClearForm();
                MessageBox.Show($"Sản phẩm '{product.Name}' đã được cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Không thể cập nhật sản phẩm. Mã sản phẩm có thể đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm để xóa.", "Yêu cầu chọn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string productName = _selectedProduct.Name;
            int productId = _selectedProduct.Id;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa sản phẩm '{productName}'?\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (DatabaseHelper.DeleteProduct(productId))
                {
                    LoadProducts();
                    ClearForm();
                    MessageBox.Show($"Sản phẩm '{productName}' đã được xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể xóa sản phẩm. Sản phẩm có thể đang được sử dụng trong hóa đơn.", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Chọn tệp CSV để nhập sản phẩm"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                int importedCount = DatabaseHelper.ImportProductsFromCsv(filePath);
                if (importedCount >= 0)
                {
                    LoadProducts();
                    MessageBox.Show($"Đã nhập thành công {importedCount} sản phẩm từ tệp CSV.", "Nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể nhập sản phẩm từ tệp CSV. Vui lòng kiểm tra định dạng tệp.", "Lỗi nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Lưu sản phẩm vào tệp CSV",
                FileName = "products.csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                bool success = DatabaseHelper.ExportProductsToCsv(filePath);
                if (success)
                {
                    MessageBox.Show("Đã xuất sản phẩm thành công sang tệp CSV.", "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể xuất sản phẩm sang tệp CSV. Vui lòng thử lại.", "Lỗi xuất", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_products.Count == 0)
            {
                MessageBox.Show("Không có sản phẩm nào để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa TẤT CẢ {_products.Count} sản phẩm?\n\nHành động này không thể hoàn tác và sẽ xóa toàn bộ dữ liệu sản phẩm.",
                "Xác nhận xóa tất cả",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (DatabaseHelper.DeleteAllProducts())
                {
                    LoadProducts();
                    ClearForm();
                    MessageBox.Show($"Đã xóa thành công tất cả {_products.Count} sản phẩm!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể xóa tất cả sản phẩm. Một số sản phẩm có thể đang được sử dụng trong hóa đơn.", "Xóa thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearForm()
        {
            ProductNameTextBox.Clear();
            ProductCodeTextBox.Clear();
            CategoryComboBox.SelectedIndex = -1;
            PriceTextBox.Clear();
            StockQuantityTextBox.Clear();
            DescriptionTextBox.Clear();
            _selectedProduct = null;
            ProductDataGrid.SelectedItem = null;
            ProductNameTextBox.Focus();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên sản phẩm.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProductNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PriceTextBox.Text) || !decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Vui lòng nhập giá hợp lệ.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(StockQuantityTextBox.Text) || !int.TryParse(StockQuantityTextBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng tồn hợp lệ.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                StockQuantityTextBox.Focus();
                return false;
            }

            return true;
        }

        private void ProductDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = ProductDataGrid.SelectedItem as ProductViewModel;
            if (_selectedProduct != null)
            {
                ProductNameTextBox.Text = _selectedProduct.Name ?? "";
                ProductCodeTextBox.Text = _selectedProduct.Code ?? "";
                CategoryComboBox.SelectedValue = _selectedProduct.CategoryId;
                PriceTextBox.Text = _selectedProduct.Price.ToString("F2");
                StockQuantityTextBox.Text = _selectedProduct.StockQuantity.ToString();
                DescriptionTextBox.Text = _selectedProduct.Description ?? "";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterProducts();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterProducts();
        }

        private void FilterProducts()
        {
            string searchTerm = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _paginationHelper.SetFilter(null!);
            }
            else
            {
                _paginationHelper.SetFilter(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Code.ToLower().Contains(searchTerm) ||
                    p.CategoryName.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm)
                );
            }
        }

        // Pagination event handlers
        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.FirstPage();
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.PreviousPage();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.NextPage();
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _paginationHelper.LastPage();
        }

        private void CurrentPageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (int.TryParse(CurrentPageTextBox.Text, out int pageNumber))
                {
                    if (!_paginationHelper.GoToPage(pageNumber))
                    {
                        // Reset to current page if invalid
                        CurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                        MessageBox.Show($"Trang không hợp lệ. Vui lòng nhập từ 1 đến {_paginationHelper.TotalPages}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    CurrentPageTextBox.Text = _paginationHelper.CurrentPage.ToString();
                }
            }
        }
        
        private void ProductDataGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
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
            Func<IEnumerable<ProductViewModel>, IOrderedEnumerable<ProductViewModel>>? sortFunc = null;
            
            switch (propertyName.ToLower())
            {
                case "id":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.Id)
                        : items => items.OrderByDescending(p => p.Id);
                    break;
                case "name":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.Name)
                        : items => items.OrderByDescending(p => p.Name);
                    break;
                case "code":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.Code)
                        : items => items.OrderByDescending(p => p.Code);
                    break;
                case "categoryname":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.CategoryName)
                        : items => items.OrderByDescending(p => p.CategoryName);
                    break;
                case "price":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.Price)
                        : items => items.OrderByDescending(p => p.Price);
                    break;
                case "stockquantity":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.StockQuantity)
                        : items => items.OrderByDescending(p => p.StockQuantity);
                    break;
                case "description":
                    sortFunc = direction == System.ComponentModel.ListSortDirection.Ascending
                        ? items => items.OrderBy(p => p.Description)
                        : items => items.OrderByDescending(p => p.Description);
                    break;
            }
            
            if (sortFunc != null)
            {
                _paginationHelper.SetSort(sortFunc);
                
                // Update column sort direction
                column.SortDirection = direction;
                
                // Clear other columns' sort direction
                foreach (var col in ProductDataGrid.Columns)
                {
                    if (col != column)
                        col.SortDirection = null;
                }
            }
        }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Description { get; set; } = "";
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
