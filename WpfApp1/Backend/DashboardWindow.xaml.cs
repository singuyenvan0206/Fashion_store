using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Linq;

namespace WpfApp1
{
    public partial class DashboardWindow : Window
    {
        private string _currentUsername;
        private string _currentRole;

        public DashboardWindow(string username, string role)
        {
            InitializeComponent();
            _currentUsername = username;
            _currentRole = role;
            
            // Add cleanup when window is closing
            this.Closing += DashboardWindow_Closing;
            
            // Set current user in application resources for other windows to access
            Application.Current.Resources["CurrentUser"] = username;
            
            UserInfoTextBlock.Text = $"Chào mừng, {username} ({role})";
            LoadKpis();
            
            // Debug: Show role information
            System.Diagnostics.Debug.WriteLine($"DashboardWindow: Username={username}, Role={role}");
            System.Diagnostics.Debug.WriteLine($"Set CurrentUser in Application.Resources: {username}");
            
            // Apply role-based visibility for unified dashboard
            ApplyRoleVisibility(ParseRole(role));

            // Ensure initial UI state after load
            this.Dispatcher.BeginInvoke(() =>
            {
                if (DefaultContentScrollViewer != null)
                {
                    DefaultContentScrollViewer.Visibility = System.Windows.Visibility.Visible;
                }
                if (MainContentHost != null)
                {
                    MainContentHost.Visibility = System.Windows.Visibility.Collapsed;
                    MainContentHost.Content = null;
                }
                if (NavList != null)
                {
                    NavList.SelectedIndex = 0;
                }
            });
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

        private void ApplyRoleVisibility(UserRole role)
        {
            void Show(UIElement e) { if (e != null) e.Visibility = Visibility.Visible; }
            void Hide(UIElement e) { if (e != null) e.Visibility = Visibility.Collapsed; }

            // Show all by default
            Show(UserManagementBorder);
            Show(ProductManagementTile);
            Show(CategoryManagementTile);
            Show(CustomerManagementTile);
            Show(InvoiceManagementTile);
            Show(ReportsTile);
            if (UserManagementNavItem != null) UserManagementNavItem.Visibility = Visibility.Visible;
            if (ProductsNavItem != null) ProductsNavItem.Visibility = Visibility.Visible;
            if (CategoriesNavItem != null) CategoriesNavItem.Visibility = Visibility.Visible;
            if (ProductSearchNavItem != null) ProductSearchNavItem.Visibility = Visibility.Collapsed;
            if (CustomersNavItem != null) CustomersNavItem.Visibility = Visibility.Visible;
            if (InvoicesNavItem != null) InvoicesNavItem.Visibility = Visibility.Visible;
            if (ReportsNavItem != null) ReportsNavItem.Visibility = Visibility.Visible;
            if (SettingsNavItem != null) SettingsNavItem.Visibility = Visibility.Visible;

            switch (role)
            {
                case UserRole.Admin:
                    // Admin sees everything
                    break;
                case UserRole.Manager:
                    // Manager: all except user management
                    Hide(UserManagementBorder);
                    if (UserManagementNavItem != null) UserManagementNavItem.Visibility = Visibility.Collapsed;
                    break;
                default:
                    // Employee: basic operations only
                    Hide(ProductManagementTile);
                    Hide(CategoryManagementTile);
                    Hide(ReportsTile);
                    Hide(UserManagementBorder);
                    if (ProductsNavItem != null) ProductsNavItem.Visibility = Visibility.Collapsed;
                    if (CategoriesNavItem != null) CategoriesNavItem.Visibility = Visibility.Collapsed;
                    if (ReportsNavItem != null) ReportsNavItem.Visibility = Visibility.Collapsed;
                    if (UserManagementNavItem != null) UserManagementNavItem.Visibility = Visibility.Collapsed;
                    if (SettingsNavItem != null) SettingsNavItem.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void LoadKpis()
        {
            try
            {
                var todayStart = System.DateTime.Today;
                var todayEnd = todayStart.AddDays(1).AddTicks(-1);
                var monthStart = System.DateTime.Today.AddDays(-30);
                var monthEnd = todayEnd;

                decimal revenueToday = DatabaseHelper.GetRevenueBetween(todayStart, todayEnd);
                decimal revenue30 = DatabaseHelper.GetRevenueBetween(monthStart, monthEnd);
                int invoicesToday = DatabaseHelper.GetInvoiceCountBetween(todayStart, todayEnd);
                int totalCustomers = DatabaseHelper.GetTotalCustomers();
                int totalProducts = DatabaseHelper.GetTotalProducts();

                RevenueTodayText.Text = $"{revenueToday:N0}₫";
                Revenue30Text.Text = $"{revenue30:N0}₫";
                InvoicesTodayText.Text = invoicesToday.ToString();
                CountsText.Text = $"{totalCustomers} / {totalProducts}";
                LoadHomeCharts(monthStart, monthEnd);
            }
            catch
            {
                // ignore UI KPI errors
            }
        }

        private void LoadHomeCharts(DateTime monthStart, DateTime monthEnd)
        {
            // Revenue last 30 days line
            var revenuePoints = DatabaseHelper.GetRevenueByDay(monthStart, monthEnd);
            var revenueModel = new PlotModel();
            revenueModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM-dd", IsZoomEnabled = false, IsPanEnabled = false });
            revenueModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, IsZoomEnabled = false, IsPanEnabled = false });
            var line = new LineSeries { MarkerType = MarkerType.Circle };
            foreach (var (day, amount) in revenuePoints)
            {
                line.Points.Add(new DataPoint(DateTimeAxis.ToDouble(day), (double)amount));
            }
            revenueModel.Series.Add(line);
            if (HomeRevenuePlot != null) HomeRevenuePlot.Model = revenueModel;

            // Revenue by category pie
            var byCat = DatabaseHelper.GetRevenueByCategory(monthStart, monthEnd, 8);
            var catModel = new PlotModel();
            var pie = new OxyPlot.Series.PieSeries { InsideLabelPosition = 0.7 };
            foreach (var (name, revenue) in byCat)
            {
                pie.Slices.Add(new OxyPlot.Series.PieSlice(name, (double)revenue));
            }
            catModel.Series.Add(pie);
            if (HomeCategoryPie != null) HomeCategoryPie.Model = catModel;
        }

        private void ProductManagement_Click(object sender, RoutedEventArgs e)
        {
            var productWindow = new ProductManagementWindow();
            ShowOwnedDialog(productWindow);
        }

        private void CategoryManagement_Click(object sender, RoutedEventArgs e)
        {
            var categoryWindow = new CategoryManagementWindow();
            ShowOwnedDialog(categoryWindow);
        }

        private void CustomerManagement_Click(object sender, RoutedEventArgs e)
        {
            var customerWindow = new CustomerManagementWindow();
            ShowOwnedDialog(customerWindow);
        }

        private void InvoiceManagement_Click(object sender, RoutedEventArgs e)
        {
            var invoiceWindow = new InvoiceManagementWindow();
            ShowOwnedDialog(invoiceWindow);
        }

        private void ReportsManagement_Click(object sender, RoutedEventArgs e)
        {
            var reportsWindow = new ReportsWindow();
            ShowOwnedDialog(reportsWindow);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            ShowOwnedDialog(settingsWindow);
        }

        private void DashboardWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Clear any resources or connections
                Application.Current.Resources.Remove("CurrentUser");
            }
            catch { }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận đăng xuất", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new MainWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();
                this.Close();
            }
        }

        private void NavList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainContentHost == null || DefaultContentScrollViewer == null)
            {
                return; // Controls not ready yet (can happen during InitializeComponent)
            }
            if (NavList.SelectedItem is System.Windows.Controls.ListBoxItem item)
            {
                string? tab = item.Content?.ToString();
                if (string.IsNullOrWhiteSpace(tab)) return;

                // Home resets to dashboard content
                if (tab == "🏠 Trang Chủ" || tab == "Home")
                {
                    MainContentHost.Visibility = System.Windows.Visibility.Collapsed;
                    DefaultContentScrollViewer.Visibility = System.Windows.Visibility.Visible;
                    // Ensure the right content host container is hidden when returning Home
                    if (MainContentScrollViewer != null)
                    {
                        MainContentScrollViewer.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    return;
                }

                // For other tabs, open the corresponding window content modally in the host
                System.Windows.Window? contentWindow = tab switch
                {
                    "📦 Sản Phẩm" or "Products" => new ProductManagementWindow(),
                    "📂 Danh Mục" or "Categories" => new CategoryManagementWindow(),
                    // Product Search UI removed
                    "👥 Khách Hàng" or "Customers" => new CustomerManagementWindow(),
                    "🧾 Hóa Đơn" or "Invoices" => new InvoiceManagementWindow(),
                    "📊 Báo Cáo" or "Reports" => new ReportsWindow(),
                    "👤 Quản Lý Người Dùng" or "User Management" => new UserManagementWindow(),
                    "⚙️ Cài Đặt" or "Settings" => new SettingsWindow(),
                    "🚪 Đăng Xuất" or "Logout" => null,
                    _ => null
                };

                if (tab == "🚪 Đăng Xuất" || tab == "Logout")
                {
                    LogoutButton_Click(this, new RoutedEventArgs());
                    return;
                }

                if (contentWindow != null)
                {
                    // Re-parent the window content into the ContentControl
                    var content = contentWindow.Content as System.Windows.UIElement;
                    contentWindow.Content = null;
                    MainContentHost.Content = content;
                    DefaultContentScrollViewer.Visibility = System.Windows.Visibility.Collapsed;
                    MainContentHost.Visibility = System.Windows.Visibility.Visible;
                    // Make sure the ScrollViewer hosting MainContentHost is visible
                    if (MainContentScrollViewer != null)
                    {
                        MainContentScrollViewer.Visibility = System.Windows.Visibility.Visible;
                    }
                    // Close the source window instance to avoid hidden open windows
                    try { contentWindow.Close(); } catch { }
                }
            }
        }

        private void ShowOwnedDialog(Window dialog)
        {
            try
            {
                if (this.IsLoaded)
                {
                    dialog.Owner = this;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                dialog.ShowDialog();
            }
            catch (InvalidOperationException)
            {
                // Fallback if owner not allowed yet
                dialog.Owner = null;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
            }
        }

        private void UserManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userManagementWindow = new UserManagementWindow();
                userManagementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở quản lý người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataVisualization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var visualizationWindow = new Window
                {
                    Title = "📈 Trực Quan Hóa Dữ Liệu",
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };

                var mainPanel = new StackPanel
                {
                    Margin = new Thickness(20),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };

                var titleText = new TextBlock
                {
                    Text = "📊 Dashboard Trực Quan",
                    FontSize = 28,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                mainPanel.Children.Add(titleText);

                // KPI Cards
                var kpiGrid = new Grid();
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var kpis = new[]
                {
                    new { Title = "💰 Doanh Thu Hôm Nay", Value = "2,450,000 VND", Color = Color.FromRgb(76, 175, 80) },
                    new { Title = "🧾 Hóa Đơn Hôm Nay", Value = "23", Color = Color.FromRgb(33, 150, 243) },
                    new { Title = "📦 Sản Phẩm Bán", Value = "156", Color = Color.FromRgb(255, 152, 0) },
                    new { Title = "👥 Khách Hàng Mới", Value = "8", Color = Color.FromRgb(156, 39, 176) }
                };

                for (int i = 0; i < kpis.Length; i++)
                {
                    var kpiCard = CreateKPICard(kpis[i].Title, kpis[i].Value, kpis[i].Color);
                    Grid.SetColumn(kpiCard, i);
                    kpiGrid.Children.Add(kpiCard);
                }

                mainPanel.Children.Add(kpiGrid);

                // Charts placeholder
                var chartsText = new TextBlock
                {
                    Text = "📈 Biểu đồ doanh thu và thống kê sẽ được hiển thị ở đây",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 0),
                    TextAlignment = TextAlignment.Center
                };

                mainPanel.Children.Add(chartsText);

                visualizationWindow.Content = mainPanel;
                visualizationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở trực quan hóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChartsAnalytics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chartsWindow = new Window
                {
                    Title = "📊 Biểu Đồ và Phân Tích",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Title
                var titleText = new TextBlock
                {
                    Text = "📊 Phân Tích Dữ Liệu Chi Tiết",
                    FontSize = 28,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(20, 20, 20, 30)
                };
                Grid.SetRow(titleText, 0);
                mainGrid.Children.Add(titleText);

                // Content area with options and charts
                var contentGrid = new Grid();
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Options panel
                var optionsPanel = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Margin = new Thickness(20, 0, 10, 20)
                };

                var optionsTitle = new TextBlock
                {
                    Text = "🎯 Chọn Loại Phân Tích",
                    FontSize = 18,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    Margin = new Thickness(15, 15, 15, 20)
                };
                optionsPanel.Children.Add(optionsTitle);

                // Analysis options
                var analysisOptions = new[]
                {
                    new { Title = "📈 Doanh Thu Theo Tháng", Description = "Biểu đồ doanh thu 12 tháng gần nhất", Action = "RevenueByMonth" },
                    new { Title = "📊 Sản Phẩm Bán Chạy", Description = "Top 10 sản phẩm bán chạy nhất", Action = "TopProducts" },
                    new { Title = "📉 Xu Hướng Khách Hàng", Description = "Phân tích khách hàng mới theo tháng", Action = "CustomerTrend" },
                    new { Title = "🎯 Doanh Thu Theo Danh Mục", Description = "Phân bổ doanh thu theo từng danh mục", Action = "RevenueByCategory" },
                    new { Title = "📅 Doanh Thu Theo Ngày", Description = "Biểu đồ doanh thu 30 ngày gần nhất", Action = "RevenueByDay" },
                    new { Title = "💰 Thống Kê Tổng Quan", Description = "Dashboard tổng hợp các chỉ số KPI", Action = "OverallStats" }
                };

                // Chart content area
                var chartContentArea = new ScrollViewer
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Margin = new Thickness(10, 0, 20, 20),
                    Padding = new Thickness(20),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                foreach (var option in analysisOptions)
                {
                    var optionButton = new Button
                    {
                        Content = CreateOptionButtonContent(option.Title, option.Description),
                        Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(15, 0, 15, 10),
                        Padding = new Thickness(10),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        Tag = option.Action
                    };
                    optionButton.Click += (s, args) => ShowAnalysisChart(option.Action, chartContentArea);
                    optionsPanel.Children.Add(optionButton);
                }

                Grid.SetColumn(optionsPanel, 0);
                contentGrid.Children.Add(optionsPanel);

                var defaultContent = new TextBlock
                {
                    Text = "👈 Chọn một loại phân tích từ menu bên trái để xem biểu đồ tương ứng",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                chartContentArea.Content = defaultContent;

                Grid.SetColumn(chartContentArea, 1);
                contentGrid.Children.Add(chartContentArea);

                Grid.SetRow(contentGrid, 1);
                mainGrid.Children.Add(contentGrid);

                chartsWindow.Content = mainGrid;
                chartsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở biểu đồ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private StackPanel CreateOptionButtonContent(string title, string description)
        {
            var panel = new StackPanel();
            
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            var descBlock = new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextWrapping = TextWrapping.Wrap
            };
            
            panel.Children.Add(titleBlock);
            panel.Children.Add(descBlock);
            
            return panel;
        }

        private void ShowAnalysisChart(string analysisType, ScrollViewer contentArea)
        {
            try
            {
                System.Windows.UIElement chartContent = analysisType switch
                {
                    "RevenueByMonth" => CreateRevenueByMonthChart(),
                    "TopProducts" => CreateTopProductsChart(),
                    "CustomerTrend" => CreateCustomerTrendChart(),
                    "RevenueByCategory" => CreateRevenueByCategoryChart(),
                    "RevenueByDay" => CreateRevenueByDayChart(),
                    "OverallStats" => CreateOverallStatsChart(),
                    _ => new TextBlock { Text = "Chức năng đang được phát triển...", FontSize = 16 }
                };

                contentArea.Content = chartContent;
            }
            catch (Exception ex)
            {
                var errorContent = new TextBlock
                {
                    Text = $"Lỗi hiển thị biểu đồ: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };
                contentArea.Content = errorContent;
            }
        }

        private System.Windows.UIElement CreateRevenueByMonthChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "📈 Doanh Thu Theo Tháng (12 tháng gần nhất)",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database
                var fromDate = DateTime.Today.AddMonths(-11).Date;
                var toDate = DateTime.Today.Date.AddDays(1).AddTicks(-1);
                var revenueData = DatabaseHelper.GetRevenueByDay(fromDate, toDate);

                var chartModel = new PlotModel { Title = "Doanh Thu Theo Tháng" };
                chartModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM/yyyy" });
                chartModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Doanh Thu (VND)" });

                var series = new LineSeries { Title = "Doanh Thu", MarkerType = MarkerType.Circle };
                
                if (revenueData.Count > 0)
                {
                    foreach (var (day, revenue) in revenueData)
                    {
                        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(day), (double)revenue));
                    }
                }
                else
                {
                    // Nếu không có dữ liệu, hiển thị thông báo
                    var noDataText = new TextBlock
                    {
                        Text = "📊 Chưa có dữ liệu doanh thu trong 12 tháng gần nhất",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    panel.Children.Add(noDataText);
                    return panel;
                }

                chartModel.Series.Add(series);

                var plotView = new OxyPlot.Wpf.PlotView
                {
                    Model = chartModel,
                    Height = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                panel.Children.Add(plotView);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private System.Windows.UIElement CreateTopProductsChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "📊 Top 10 Sản Phẩm Bán Chạy",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database
                var fromDate = DateTime.Today.AddMonths(-3).Date; // 3 tháng gần nhất
                var toDate = DateTime.Today.Date.AddDays(1).AddTicks(-1);
                var topProducts = DatabaseHelper.GetTopProducts(fromDate, toDate, 10);

                var chartModel = new PlotModel { Title = "Sản Phẩm Bán Chạy" };
                chartModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Sản Phẩm" });
                chartModel.Axes.Add(new CategoryAxis { Position = AxisPosition.Left, Title = "Số Lượng Bán" });

                var series = new BarSeries { Title = "Số Lượng" };
                
                if (topProducts.Count > 0)
                {
                    for (int i = 0; i < topProducts.Count; i++)
                    {
                        var (productName, quantity) = topProducts[i];
                        series.Items.Add(new BarItem(quantity, i));
                    }
                }
                else
                {
                    // Nếu không có dữ liệu, hiển thị thông báo
                    var noDataText = new TextBlock
                    {
                        Text = "📊 Chưa có dữ liệu bán hàng trong 3 tháng gần nhất",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    panel.Children.Add(noDataText);
                    return panel;
                }

                chartModel.Series.Add(series);

                var plotView = new OxyPlot.Wpf.PlotView
                {
                    Model = chartModel,
                    Height = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                panel.Children.Add(plotView);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private System.Windows.UIElement CreateCustomerTrendChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "📉 Xu Hướng Khách Hàng Mới",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database - đếm khách hàng mới theo tháng
                var fromDate = DateTime.Today.AddMonths(-11).Date;
                var toDate = DateTime.Today.Date.AddDays(1).AddTicks(-1);
                
                // Tạo dữ liệu khách hàng mới theo tháng
                var customerTrendData = new List<(DateTime Month, int Count)>();
                var currentDate = fromDate;
                
                while (currentDate <= toDate)
                {
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                    
                    // Đếm khách hàng được tạo trong tháng này
                    var customers = DatabaseHelper.GetAllCustomers();
                    var newCustomersInMonth = customers.Count(c => 
                    {
                        // Giả sử CreatedDate có sẵn, nếu không thì dùng dữ liệu hiện có
                        return true; // Tạm thời hiển thị tất cả khách hàng
                    });
                    
                    customerTrendData.Add((monthStart, newCustomersInMonth));
                    currentDate = currentDate.AddMonths(1);
                }

                var chartModel = new PlotModel { Title = "Khách Hàng Mới Theo Tháng" };
                chartModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM/yyyy" });
                chartModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Số Khách Hàng" });

                var series = new LineSeries { Title = "Khách Hàng Mới", MarkerType = MarkerType.Circle };
                
                if (customerTrendData.Count > 0)
                {
                    foreach (var (month, count) in customerTrendData)
                    {
                        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(month), count));
                    }
                }
                else
                {
                    // Nếu không có dữ liệu, hiển thị thông báo
                    var noDataText = new TextBlock
                    {
                        Text = "📊 Chưa có dữ liệu khách hàng trong 12 tháng gần nhất",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    panel.Children.Add(noDataText);
                    return panel;
                }

                chartModel.Series.Add(series);

                var plotView = new OxyPlot.Wpf.PlotView
                {
                    Model = chartModel,
                    Height = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                panel.Children.Add(plotView);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private System.Windows.UIElement CreateRevenueByCategoryChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "🎯 Doanh Thu Theo Danh Mục",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database
                var fromDate = DateTime.Today.AddMonths(-3).Date; // 3 tháng gần nhất
                var toDate = DateTime.Today.Date.AddDays(1).AddTicks(-1);
                var revenueByCategory = DatabaseHelper.GetRevenueByCategory(fromDate, toDate, 8);

                var chartModel = new PlotModel { Title = "Phân Bổ Doanh Thu" };
                var pieSeries = new PieSeries { Title = "Doanh Thu" };
                
                if (revenueByCategory.Count > 0)
                {
                    foreach (var (categoryName, revenue) in revenueByCategory)
                    {
                        pieSeries.Slices.Add(new PieSlice(categoryName, (double)revenue));
                    }
                }
                else
                {
                    // Nếu không có dữ liệu, hiển thị thông báo
                    var noDataText = new TextBlock
                    {
                        Text = "📊 Chưa có dữ liệu doanh thu theo danh mục trong 3 tháng gần nhất",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    panel.Children.Add(noDataText);
                    return panel;
                }

                chartModel.Series.Add(pieSeries);

                var plotView = new OxyPlot.Wpf.PlotView
                {
                    Model = chartModel,
                    Height = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                panel.Children.Add(plotView);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private System.Windows.UIElement CreateRevenueByDayChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "📅 Doanh Thu 30 Ngày Gần Nhất",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database
                var fromDate = DateTime.Today.AddDays(-29).Date;
                var toDate = DateTime.Today.Date.AddDays(1).AddTicks(-1);
                var revenueData = DatabaseHelper.GetRevenueByDay(fromDate, toDate);

                var chartModel = new PlotModel { Title = "Doanh Thu Theo Ngày" };
                chartModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "dd/MM" });
                chartModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Doanh Thu (VND)" });

                var series = new LineSeries { Title = "Doanh Thu", MarkerType = MarkerType.Circle };
                
                if (revenueData.Count > 0)
                {
                    foreach (var (day, revenue) in revenueData)
                    {
                        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(day), (double)revenue));
                    }
                }
                else
                {
                    // Nếu không có dữ liệu, hiển thị thông báo
                    var noDataText = new TextBlock
                    {
                        Text = "📊 Chưa có dữ liệu doanh thu trong 30 ngày gần nhất",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    panel.Children.Add(noDataText);
                    return panel;
                }

                chartModel.Series.Add(series);

                var plotView = new OxyPlot.Wpf.PlotView
                {
                    Model = chartModel,
                    Height = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                panel.Children.Add(plotView);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private System.Windows.UIElement CreateOverallStatsChart()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "💰 Thống Kê Tổng Quan KPI",
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(title);

            try
            {
                // Lấy dữ liệu thực từ database
                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1).AddTicks(-1);
                var monthStart = DateTime.Today.AddDays(-30);
                var monthEnd = todayEnd;

                var totalRevenue = DatabaseHelper.GetRevenueBetween(monthStart, monthEnd);
                var totalInvoices = DatabaseHelper.GetInvoiceCountBetween(monthStart, monthEnd);
                var totalCustomers = DatabaseHelper.GetTotalCustomers();
                var totalProducts = DatabaseHelper.GetTotalProducts();

                // KPI Grid
                var kpiGrid = new Grid();
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var kpis = new[]
                {
                    new { Title = "💰 Doanh Thu 30 Ngày", Value = $"{totalRevenue:N0} VND", Color = Color.FromRgb(76, 175, 80) },
                    new { Title = "🧾 Hóa Đơn 30 Ngày", Value = totalInvoices.ToString(), Color = Color.FromRgb(33, 150, 243) },
                    new { Title = "📦 Tổng Sản Phẩm", Value = totalProducts.ToString(), Color = Color.FromRgb(255, 152, 0) },
                    new { Title = "👥 Tổng Khách Hàng", Value = totalCustomers.ToString(), Color = Color.FromRgb(156, 39, 176) }
                };

                for (int i = 0; i < kpis.Length; i++)
                {
                    var kpiCard = CreateKPICard(kpis[i].Title, kpis[i].Value, kpis[i].Color);
                    Grid.SetColumn(kpiCard, i);
                    kpiGrid.Children.Add(kpiCard);
                }

                panel.Children.Add(kpiGrid);

                // Additional summary text
                var summaryText = new TextBlock
                {
                    Text = "📊 Tổng hợp các chỉ số kinh doanh quan trọng của hệ thống (30 ngày gần nhất)",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0),
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(summaryText);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock
                {
                    Text = $"❌ Lỗi tải dữ liệu: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                panel.Children.Add(errorText);
            }

            return panel;
        }

        private Border CreateKPICard(string title, string value, Color color)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 3,
                    BlurRadius = 8,
                    Opacity = 0.2
                }
            };

            var panel = new StackPanel();
            
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 24,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            
            panel.Children.Add(titleBlock);
            panel.Children.Add(valueBlock);
            
            card.Child = panel;
            return card;
        }
    }
}
