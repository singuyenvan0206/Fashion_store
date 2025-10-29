## Sale Management (WPF/.NET 8)

Ứng dụng quản lý bán hàng xây dựng bằng WPF trên .NET 8, hỗ trợ quản lý người dùng, sản phẩm, khách hàng, hoá đơn, báo cáo và tuỳ chỉnh cài đặt hệ thống.

### Tính năng chính
- Quản lý người dùng, phân quyền
- Quản lý danh mục, sản phẩm, khách hàng
- Lập hoá đơn, in hoá đơn, lịch sử giao dịch
- Báo cáo tổng hợp, tuỳ chỉnh mẫu báo cáo
- Cài đặt mức giá/tier, phương thức thanh toán, mã QR

### Yêu cầu hệ thống
- Windows 10/11
- .NET SDK 8.0 trở lên (để build)
- Visual Studio 2022 (khuyến nghị) với workload .NET Desktop Development

### Cấu trúc thư mục
```
Sale_management/
  WpfApp1/                 // Mã nguồn chính của ứng dụng WPF
    Views/                 // XAML views (cửa sổ giao diện)
    Backend/               // Logic nghiệp vụ, truy cập dữ liệu, helpers
    WpfApp1.csproj
  WpfApp1.sln              // Solution
  Class_Diagram.md         // Sơ đồ lớp (tham khảo)
```

### Thiết lập & Chạy
1) Mở solution
- Mở `WpfApp1.sln` bằng Visual Studio 2022.

2) Khôi phục gói
- Visual Studio sẽ tự khôi phục NuGet. Nếu cần, vào `Tools > NuGet Package Manager > Package Manager Console` và chạy:
```powershell
Update-Package -reinstall
```

3) Cấu hình cơ sở dữ liệu (nếu áp dụng)
- Mặc định dự án bao gồm thư viện cho SQLite và MySQL (`System.Data.SQLite`, `SQLitePCLRaw`, `MySql.Data`).
- Kiểm tra hoặc chỉnh chuỗi kết nối, cơ chế khởi tạo DB tại `WpfApp1/Backend/DatabaseHelper.cs`.
- Nếu dùng SQLite: đảm bảo file DB (nếu có) nằm tại vị trí đã cấu hình; nếu không, ứng dụng có thể tự tạo khi chạy lần đầu.
- Nếu dùng MySQL: đảm bảo server hoạt động và user có quyền truy cập theo chuỗi kết nối.

4) Chạy Debug
- Chọn cấu hình `Debug` và `WpfApp1` làm startup project.
- Nhấn F5 để chạy.

### Build Release / Publish
- Trong Visual Studio: `Build > Publish` để tạo gói phát hành (self-contained hoặc framework-dependent) cho `net8.0-windows`.
- Khuyến nghị target `win-x64` và `Use WPF` đã bật trong csproj.

### Các cửa sổ chính (Views)
- `MainWindow.xaml`: điểm vào ứng dụng
- `DashboardWindow.xaml`: bảng điều khiển
- `ProductManagementWindow.xaml`: quản lý sản phẩm
- `CustomerManagementWindow.xaml`: quản lý khách hàng
- `InvoiceManagementWindow.xaml` / `InvoicePrintWindow.xaml`: hoá đơn & in hoá đơn
- `ReportsWindow.xaml` / `ReportsSettingsWindow.xaml`: báo cáo & cấu hình báo cáo
- `SettingsWindow.xaml`: cài đặt chung
- `UserManagementWindow.xaml` / `AddEditUserWindow.xaml` / `ChangePasswordWindow.xaml`: người dùng & bảo mật
- `CategoryManagementWindow.xaml`, `TransactionHistoryWindow.xaml`, `TierSettingsWindow.xaml`

### Backend đáng chú ý
- `Backend/DatabaseHelper.cs`: lớp truy cập dữ liệu trung tâm, tạo bảng, CRUD, truy vấn phân trang, v.v.
- `Backend/PaginationHelper.cs`: hỗ trợ phân trang
- `Backend/QRCodeHelper.cs`: hỗ trợ tạo mã QR
- `Backend/SettingsManager.cs`, `PaymentSettings.cs`, `TierSettings.cs`: quản lý cấu hình
- `Backend/UserRole.cs`: định nghĩa phân quyền

### Ghi chú phát triển
- Sử dụng .NET 8: `TargetFramework` là `net8.0-windows` trong `WpfApp1.csproj`.
- Nếu gặp lỗi thiếu native SQLite, đảm bảo `SQLitePCLRaw.provider.e_sqlite3` được copy cùng output hoặc cài đặt runtime phù hợp.
- Khi thay đổi mô hình dữ liệu, xem lại các phương thức trong `DatabaseHelper` để đảm bảo đồng bộ tạo bảng/migration tối thiểu.

### Tài liệu liên quan
- Sơ đồ lớp: xem `Class_Diagram.md`.

### Giấy phép
Đính kèm giấy phép của bạn tại đây (MIT/GPL/Commercial…).


