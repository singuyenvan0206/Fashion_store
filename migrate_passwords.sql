-- Script SQL để mã hóa mật khẩu trong database
-- LƯU Ý: Script này chỉ để tham khảo. 
-- Nên sử dụng ứng dụng WPF để chạy migration vì cần mã hóa bằng SHA256 với salt.

-- Script này sẽ KHÔNG hoạt động trực tiếp vì MySQL không có hàm SHA256 với salt built-in.
-- Thay vào đó, hãy sử dụng cửa sổ PasswordMigrationWindow trong ứng dụng.

-- Để kiểm tra mật khẩu nào chưa được mã hóa (mật khẩu đã mã hóa có độ dài 64 ký tự Base64):
-- SELECT Id, Username, 
--        CASE 
--            WHEN LENGTH(Password) = 64 AND Password REGEXP '^[A-Za-z0-9+/=]+$' THEN 'Đã mã hóa'
--            ELSE 'Chưa mã hóa'
--        END AS Status
-- FROM Accounts;

-- Sau khi chạy migration từ ứng dụng, bạn có thể kiểm tra lại:
-- SELECT Id, Username, LENGTH(Password) AS PasswordLength FROM Accounts;

