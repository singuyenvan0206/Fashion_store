# Sơ Đồ Thiết Kế Lớp - Hệ Thống Quản Lý Bán Hàng

## UML Class Diagram

```mermaid
classDiagram
    %% Core Data Models
    class Account {
        +int Id
        +string Username
        +string Password
        +UserRole Role
        +string EmployeeName
        +bool IsActive
        +Login()
        +Logout()
        +HasPermission()
    }

    class UserRole {
        <<enumeration>>
        Admin
        Manager
        Cashier
    }

    class UserRoleExtensions {
        <<static>>
        +CanManageProducts(UserRole role) bool
        +CanManageCategories(UserRole role) bool
        +CanManageCustomers(UserRole role) bool
        +CanCreateInvoices(UserRole role) bool
        +CanViewReports(UserRole role) bool
    }

    class Category {
        +int Id
        +string Name
        +DateTime CreatedDate
    }

    class Product {
        +int Id
        +string Name
        +string Code
        +int CategoryId
        +decimal SalePrice
        +decimal PurchasePrice
        +string PurchaseUnit
        +int ImportQuantity
        +int StockQuantity
        +string Description
        +DateTime CreatedDate
        +DateTime UpdatedDate
        +UpdateStock(int quantity)
        +IsAvailable() bool
        +GetPrice() decimal
    }

    class Customer {
        +int Id
        +string Name
        +string Phone
        +string Email
        +string CustomerType
        +string Address
        +int Points
        +string Tier
        +DateTime CreatedDate
        +AddPoints(int points)
        +UpgradeRank()
        +GetHistory()
    }

    class Invoice {
        +int Id
        +int CustomerId
        +int EmployeeId
        +decimal Subtotal
        +decimal TaxPercent
        +decimal TaxAmount
        +decimal Discount
        +decimal Total
        +decimal Paid
        +DateTime CreatedDate
        +AddItem(InvoiceItem item)
        +Calculate()
        +Pay()
    }

    class InvoiceItem {
        +int InvoiceId
        +int ProductId
        +int Quantity
        +decimal UnitPrice
        +decimal LineTotal
        +Calculate()
        +Update()
    }

    %% Settings Classes
    class SettingsConfig {
        +string Server
        +string Database
        +string UserId
        +string Password
    }

    class PaymentSettings {
        +string BankAccount
        +string BankCode
        +string BankName
        +string AccountHolder
        +bool EnableQRCode
    }

    class TierSettings {
        +decimal RegularDiscount
        +decimal SilverDiscount
        +decimal GoldDiscount
        +decimal PlatinumDiscount
        +int SilverThreshold
        +int GoldThreshold
        +int PlatinumThreshold
    }

    %% Manager Classes
    class SettingsManager {
        <<static>>
        +Load() SettingsConfig
        +Save(SettingsConfig config, out string error) bool
        +BuildConnectionString() string
        +TestConnection(SettingsConfig cfg, out string message) bool
    }

    class PaymentSettingsManager {
        <<static>>
        +Load() PaymentSettings
        +Save(PaymentSettings settings) bool
    }

    class TierSettingsManager {
        <<static>>
        +Load() TierSettings
        +Save(TierSettings settings) bool
        +GetTierDiscount(string tier) decimal
        +DetermineTierByPoints(int points) string
        +UpdateAllCustomerTiers() int
    }

    %% Core Database Helper
    class DatabaseHelper {
        <<static>>
        +string ConnectionString
        +InitializeDatabase()
        +ValidateLogin(string username, string password) string
        +GetUserRoleEnum(string username) UserRole
        +RegisterAccount(string username, string employeeName, string password, string role) bool
        +ChangePassword(string username, string oldPassword, string newPassword) bool
        +GetAllAccounts() List~Account~
        +GetEmployeeIdByUsername(string username) int
        +DeleteAccount(string username) bool
        +DeleteAllAccountsExceptAdmin() bool
        +AddCategory(string name) bool
        +GetAllCategories() List~Category~
        +UpdateCategory(int id, string name) bool
        +DeleteCategory(int id) bool
        +DeleteAllCategories() bool
        +AddProduct(string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int importQuantity, int stockQuantity, string description) bool
        +GetAllProducts() List~Product~
        +UpdateProduct(int id, string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int importQuantity, int stockQuantity, string description) bool
        +DeleteProduct(int id) bool
        +DeleteAllProducts() bool
        +AddCustomer(string name, string phone, string email, string customerType, string address) int
        +GetAllCustomers() List~Customer~
        +UpdateCustomer(int id, string name, string phone, string email, string customerType, string address) bool
        +DeleteCustomer(int id) bool
        +DeleteAllCustomers() bool
        +SaveInvoice(int customerId, int employeeId, decimal subtotal, decimal taxPercent, decimal taxAmount, decimal discount, decimal total, decimal paid, List~InvoiceItem~ items, DateTime? createdDate, int? invoiceId) bool
        +GetAllInvoices() List~Invoice~
        +QueryInvoices(DateTime? startDate, DateTime? endDate, int? customerId, string searchTerm) List~Invoice~
        +DeleteAllInvoices() bool
        +GetCustomerLoyalty(int customerId) (string tier, int points)
        +UpdateCustomerLoyalty(int customerId, int points, string tier) bool
        +ImportProductsFromCsv(string filePath) int
        +ExportProductsToCsv(string filePath) bool
        +ImportInvoicesFromCsv(string filePath) int
        +ExportInvoicesToCsv(string filePath) bool
        +GetTotalInvoices() int
        +GetTotalRevenue() decimal
        +GetInvoiceDateRange() (DateTime? oldestDate, DateTime? newestDate)
    }

    %% Utility Classes
    class QRCodeHelper {
        <<static>>
        +HttpClient _http
        +GenerateVietQRCode_Safe(string bankCode, string bankAccount, decimal amount, string description, bool includeQueryDownload, int size, string accountHolder) BitmapSource
        +GenerateFallbackQRCode(string bankCode, string bankAccount, long amount, string description, int size, string accountHolder) BitmapSource
        +CreateErrorQRCode(string message, int size) BitmapSource
    }

    class PaginationHelper~T~ {
        +List~T~ Items
        +int PageSize
        +int CurrentPage
        +int TotalPages
        +int TotalItems
        +List~T~ GetCurrentPageItems()
        +bool HasNextPage()
        +bool HasPreviousPage()
        +void NextPage()
        +void PreviousPage()
        +void GoToPage(int page)
    }

    %% UI Window Classes
    class MainWindow {
        +MainWindow()
        +LoginButton_Click(object sender, RoutedEventArgs e)
        +ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    }

    class DashboardWindow {
        -string _currentUsername
        -string _currentRole
        -bool _isRefreshing
        +static event Action OnDashboardRefreshNeeded
        +DashboardWindow(string username, string role)
        +LoadKpis()
        +ApplyRoleVisibility(UserRole role)
        +TriggerDashboardRefresh()
    }

    class ProductManagementWindow {
        -List~ProductViewModel~ _products
        -List~CategoryViewModel~ _categories
        -ProductViewModel _selectedProduct
        +ProductManagementWindow()
        +LoadProducts()
        +LoadCategories()
        +AddButton_Click(object sender, RoutedEventArgs e)
        +UpdateButton_Click(object sender, RoutedEventArgs e)
        +DeleteButton_Click(object sender, RoutedEventArgs e)
        +DeleteAllButton_Click(object sender, RoutedEventArgs e)
        +ImportCsvButton_Click(object sender, RoutedEventArgs e)
        +ExportCsvButton_Click(object sender, RoutedEventArgs e)
    }

    class CategoryManagementWindow {
        -List~(int Id, string Name)~ _categories
        +CategoryManagementWindow()
        +LoadCategories()
        +SetupPlaceholder()
        +AddButton_Click(object sender, RoutedEventArgs e)
        +UpdateButton_Click(object sender, RoutedEventArgs e)
        +DeleteButton_Click(object sender, RoutedEventArgs e)
        +DeleteAllButton_Click(object sender, RoutedEventArgs e)
    }

    class CustomerManagementWindow {
        -List~CustomerViewModel~ _customers
        -CustomerViewModel _selectedCustomer
        +CustomerManagementWindow()
        +LoadCustomers()
        +AddButton_Click(object sender, RoutedEventArgs e)
        +UpdateButton_Click(object sender, RoutedEventArgs e)
        +DeleteButton_Click(object sender, RoutedEventArgs e)
        +DeleteAllCustomersButton_Click(object sender, RoutedEventArgs e)
    }

    class InvoiceManagementWindow {
        -List~InvoiceItemViewModel~ _invoiceItems
        +InvoiceManagementWindow()
        +LoadCustomers()
        +LoadProducts()
        +ClearInvoice()
        +UpdateTotals()
        +InitializeQRCode()
        +AddProductButton_Click(object sender, RoutedEventArgs e)
        +RemoveItemButton_Click(object sender, RoutedEventArgs e)
        +SaveInvoiceButton_Click(object sender, RoutedEventArgs e)
        +ShowPrintWindow(int invoiceId)
        +UpdateCustomerLoyalty(int customerId)
    }

    class InvoicePrintWindow {
        -List~InvoiceItemViewModel~ _items
        -CustomerListItem _customer
        -InvoiceListItem _invoice
        -int _employeeId
        +InvoicePrintWindow(int invoiceId, int employeeId)
        +PrintButton_Click(object sender, RoutedEventArgs e)
        +GenerateQRCode()
    }

    class TransactionHistoryWindow {
        -List~InvoiceSummaryItem~ _invoices
        +TransactionHistoryWindow()
        +LoadInvoices()
        +InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    }

    class ReportsWindow {
        -List~InvoiceListItem~ _invoices
        -PaginationHelper~InvoiceListItem~ _paginationHelper
        +ReportsWindow()
        +LoadInvoices()
        +ApplyFilters()
        +ExportToCsvButton_Click(object sender, RoutedEventArgs e)
        +PrintReportButton_Click(object sender, RoutedEventArgs e)
    }

    class ReportsSettingsWindow {
        +ReportsSettingsWindow()
        +LoadDatabaseStatistics()
        +ImportInvoicesCsvButton_Click(object sender, RoutedEventArgs e)
        +ExportInvoicesCsvButton_Click(object sender, RoutedEventArgs e)
        +DeleteAllInvoicesButton_Click(object sender, RoutedEventArgs e)
    }

    class UserManagementWindow {
        -List~UserManagementItem~ _allUsers
        -List~UserManagementItem~ _filteredUsers
        +UserManagementWindow()
        +LoadUsers()
        +AddUserButton_Click(object sender, RoutedEventArgs e)
        +EditUserButton_Click(object sender, RoutedEventArgs e)
        +DeleteUserButton_Click(object sender, RoutedEventArgs e)
        +DeleteAllUsersButton_Click(object sender, RoutedEventArgs e)
    }

    class AddEditUserWindow {
        -UserManagementItem _user
        -bool _isEditMode
        +AddEditUserWindow(UserManagementItem user, bool isEditMode)
        +SaveButton_Click(object sender, RoutedEventArgs e)
        +CancelButton_Click(object sender, RoutedEventArgs e)
    }

    class SettingsWindow {
        +SettingsWindow()
        +TestConnectionButton_Click(object sender, RoutedEventArgs e)
        +SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        +SavePaymentSettingsButton_Click(object sender, RoutedEventArgs e)
    }

    class TierSettingsWindow {
        +TierSettingsWindow()
        +LoadCurrentSettings()
        +SaveButton_Click(object sender, RoutedEventArgs e)
        +ResetToDefaultButton_Click(object sender, RoutedEventArgs e)
    }

    class ChangePasswordWindow {
        +ChangePasswordWindow()
        +ChangePassword_Click(object sender, RoutedEventArgs e)
        +Cancel_Click(object sender, RoutedEventArgs e)
    }

    %% ViewModel Classes
    class ProductViewModel {
        +int Id
        +string Name
        +string Code
        +string CategoryName
        +decimal SalePrice
        +decimal PurchasePrice
        +string PurchaseUnit
        +int ImportQuantity
        +int StockQuantity
        +string Description
        +DateTime CreatedDate
        +DateTime UpdatedDate
    }

    class CategoryViewModel {
        +int Id
        +string Name
        +DateTime CreatedDate
    }

    class CustomerViewModel {
        +int Id
        +string Name
        +string Phone
        +string Email
        +string CustomerType
        +string Address
        +int Points
        +string Tier
        +DateTime CreatedDate
    }

    class InvoiceItemViewModel {
        +int ProductId
        +string ProductName
        +string ProductCode
        +int Quantity
        +decimal UnitPrice
        +decimal LineTotal
    }

    class InvoiceListItem {
        +int Id
        +string CustomerName
        +string CustomerPhone
        +decimal Total
        +DateTime CreatedDate
        +string Status
    }

    class InvoiceSummaryItem {
        +int RowNumber
        +int InvoiceId
        +string CreatedAt
        +string CustomerName
        +string Total
    }

    class UserManagementItem {
        +int Id
        +string Username
        +string EmployeeName
        +string Role
        +bool IsActive
    }

    class CustomerListItem {
        +int Id
        +string Name
        +string Phone
        +string Email
        +string CustomerType
        +string Address
        +int Points
        +string Tier
    }

    %% Relationships
    Account ||--|| UserRole : has
    UserRole ||--o{ UserRoleExtensions : extends
    
    Category ||--o{ Product : contains
    Product ||--o{ InvoiceItem : referenced by
    Customer ||--o{ Invoice : creates
    Invoice ||--o{ InvoiceItem : contains
    
    DatabaseHelper ..> Account : manages
    DatabaseHelper ..> Category : manages
    DatabaseHelper ..> Product : manages
    DatabaseHelper ..> Customer : manages
    DatabaseHelper ..> Invoice : manages
    DatabaseHelper ..> InvoiceItem : manages
    
    SettingsManager ..> SettingsConfig : manages
    PaymentSettingsManager ..> PaymentSettings : manages
    TierSettingsManager ..> TierSettings : manages
    
    QRCodeHelper ..> PaymentSettings : uses
    
    PaginationHelper~T~ ..> InvoiceListItem : paginates
    
    MainWindow ..> DatabaseHelper : uses
    MainWindow ..> DashboardWindow : creates
    
    DashboardWindow ..> ProductManagementWindow : creates
    DashboardWindow ..> CategoryManagementWindow : creates
    DashboardWindow ..> CustomerManagementWindow : creates
    DashboardWindow ..> InvoiceManagementWindow : creates
    DashboardWindow ..> TransactionHistoryWindow : creates
    DashboardWindow ..> ReportsWindow : creates
    DashboardWindow ..> ReportsSettingsWindow : creates
    DashboardWindow ..> UserManagementWindow : creates
    DashboardWindow ..> SettingsWindow : creates
    DashboardWindow ..> TierSettingsWindow : creates
    DashboardWindow ..> ChangePasswordWindow : creates
    
    ProductManagementWindow ..> DatabaseHelper : uses
    ProductManagementWindow ..> ProductViewModel : uses
    ProductManagementWindow ..> CategoryViewModel : uses
    
    CategoryManagementWindow ..> DatabaseHelper : uses
    
    CustomerManagementWindow ..> DatabaseHelper : uses
    CustomerManagementWindow ..> CustomerViewModel : uses
    
    InvoiceManagementWindow ..> DatabaseHelper : uses
    InvoiceManagementWindow ..> InvoiceItemViewModel : uses
    InvoiceManagementWindow ..> QRCodeHelper : uses
    InvoiceManagementWindow ..> PaymentSettingsManager : uses
    InvoiceManagementWindow ..> TierSettingsManager : uses
    InvoiceManagementWindow ..> InvoicePrintWindow : creates
    
    InvoicePrintWindow ..> DatabaseHelper : uses
    InvoicePrintWindow ..> QRCodeHelper : uses
    InvoicePrintWindow ..> PaymentSettingsManager : uses
    
    TransactionHistoryWindow ..> DatabaseHelper : uses
    TransactionHistoryWindow ..> InvoiceSummaryItem : uses
    
    ReportsWindow ..> DatabaseHelper : uses
    ReportsWindow ..> InvoiceListItem : uses
    ReportsWindow ..> PaginationHelper : uses
    
    ReportsSettingsWindow ..> DatabaseHelper : uses
    
    UserManagementWindow ..> DatabaseHelper : uses
    UserManagementWindow ..> UserManagementItem : uses
    UserManagementWindow ..> AddEditUserWindow : creates
    
    AddEditUserWindow ..> DatabaseHelper : uses
    AddEditUserWindow ..> UserManagementItem : uses
    
    SettingsWindow ..> SettingsManager : uses
    SettingsWindow ..> PaymentSettingsManager : uses
    
    TierSettingsWindow ..> TierSettingsManager : uses
    
    ChangePasswordWindow ..> DatabaseHelper : uses
```

## Mô Tả Kiến Trúc

### 1. **Lớp Dữ Liệu (Data Models)**
- **Account**: Quản lý thông tin tài khoản người dùng
- **UserRole**: Enum định nghĩa các vai trò (Admin, Manager, Cashier)
- **Category**: Danh mục sản phẩm
- **Product**: Thông tin sản phẩm
- **Customer**: Thông tin khách hàng
- **Invoice**: Hóa đơn bán hàng
- **InvoiceItem**: Chi tiết từng sản phẩm trong hóa đơn

### 2. **Lớp Cài Đặt (Settings)**
- **SettingsConfig**: Cấu hình kết nối database
- **PaymentSettings**: Cài đặt thanh toán QR
- **TierSettings**: Cài đặt hạng thành viên

### 3. **Lớp Quản Lý (Managers)**
- **DatabaseHelper**: Quản lý tất cả thao tác database
- **SettingsManager**: Quản lý cài đặt hệ thống
- **PaymentSettingsManager**: Quản lý cài đặt thanh toán
- **TierSettingsManager**: Quản lý cài đặt hạng thành viên

### 4. **Lớp Tiện Ích (Utilities)**
- **QRCodeHelper**: Tạo mã QR thanh toán
- **PaginationHelper**: Hỗ trợ phân trang dữ liệu

### 5. **Lớp Giao Diện (UI Windows)**
- **MainWindow**: Màn hình đăng nhập
- **DashboardWindow**: Màn hình chính điều hướng
- **ProductManagementWindow**: Quản lý sản phẩm
- **CategoryManagementWindow**: Quản lý danh mục
- **CustomerManagementWindow**: Quản lý khách hàng
- **InvoiceManagementWindow**: Tạo hóa đơn
- **InvoicePrintWindow**: In hóa đơn
- **TransactionHistoryWindow**: Lịch sử giao dịch
- **ReportsWindow**: Báo cáo
- **ReportsSettingsWindow**: Cài đặt báo cáo
- **UserManagementWindow**: Quản lý người dùng
- **SettingsWindow**: Cài đặt hệ thống
- **TierSettingsWindow**: Cài đặt hạng thành viên
- **ChangePasswordWindow**: Đổi mật khẩu

### 6. **Lớp ViewModel**
- **ProductViewModel**: Dữ liệu hiển thị sản phẩm
- **CategoryViewModel**: Dữ liệu hiển thị danh mục
- **CustomerViewModel**: Dữ liệu hiển thị khách hàng
- **InvoiceItemViewModel**: Dữ liệu hiển thị chi tiết hóa đơn
- **InvoiceListItem**: Dữ liệu hiển thị danh sách hóa đơn
- **InvoiceSummaryItem**: Dữ liệu hiển thị tóm tắt hóa đơn
- **UserManagementItem**: Dữ liệu hiển thị quản lý người dùng
- **CustomerListItem**: Dữ liệu hiển thị danh sách khách hàng

## Đặc Điểm Kiến Trúc

1. **Mô hình MVC**: Tách biệt rõ ràng giữa Model (DatabaseHelper), View (Windows), và Controller (Event Handlers)

2. **Singleton Pattern**: DatabaseHelper sử dụng static methods để đảm bảo một instance duy nhất

3. **Repository Pattern**: DatabaseHelper đóng vai trò repository cho tất cả thao tác dữ liệu

4. **Settings Pattern**: Các Manager classes quản lý cài đặt một cách tập trung

5. **ViewModel Pattern**: Tách biệt dữ liệu hiển thị khỏi business logic

6. **Event-Driven Updates**: DashboardWindow sử dụng events để cập nhật real-time

7. **Role-Based Access Control**: UserRole và UserRoleExtensions quản lý phân quyền
