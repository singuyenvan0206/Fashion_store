using MySql.Data.MySqlClient;

namespace WpfApp1
{
    public static class DatabaseHelper
    {
        // Connection string is dynamically constructed from SettingsManager
        private static string ConnectionString => SettingsManager.BuildConnectionString();

        public static void InitializeDatabase()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Accounts table (already present)
            string tableCmd = @"CREATE TABLE IF NOT EXISTS Accounts (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Username VARCHAR(255) NOT NULL UNIQUE,
                Password VARCHAR(255) NOT NULL,
                Role VARCHAR(20) NOT NULL DEFAULT 'Cashier'
            );";
            using var cmd = new MySqlCommand(tableCmd, connection);
            cmd.ExecuteNonQuery();

            // Create default admin account if not exists
            string checkAdminCmd2 = "SELECT COUNT(*) FROM Accounts WHERE Username = 'admin';";
            using var checkAdmin2 = new MySqlCommand(checkAdminCmd2, connection);
            int adminExists2 = Convert.ToInt32(checkAdmin2.ExecuteScalar());

            if (adminExists2 == 0)
            {
                string createAdminCmd = "INSERT INTO Accounts (Username, Password, Role) VALUES ('admin', 'admin123', 'Admin');";
                using var createAdmin = new MySqlCommand(createAdminCmd, connection);
                createAdmin.ExecuteNonQuery();
            }

            // Categories table
            string categoryCmd = @"CREATE TABLE IF NOT EXISTS Categories (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255) NOT NULL UNIQUE
            );";
            using var catCmd = new MySqlCommand(categoryCmd, connection);
            catCmd.ExecuteNonQuery();

            // Products table
            string productCmd = @"CREATE TABLE IF NOT EXISTS Products (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255) NOT NULL,
                Code VARCHAR(50) UNIQUE,
                CategoryId INT,
                SalePrice DECIMAL(10,2) NOT NULL,
                StockQuantity INT NOT NULL DEFAULT 0,
                Description TEXT,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );";
            using var prodCmd = new MySqlCommand(productCmd, connection);
            prodCmd.ExecuteNonQuery();

            // Customers table
            string customerCmd = @"CREATE TABLE IF NOT EXISTS Customers (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255) NOT NULL,
                Phone VARCHAR(20),
                Email VARCHAR(255),
                Address TEXT,
                CustomerType VARCHAR(50) DEFAULT 'Regular',
                Points INT NOT NULL DEFAULT 0,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            );";
            using var custCmd = new MySqlCommand(customerCmd, connection);
            custCmd.ExecuteNonQuery();

            // Ensure Points column exists for legacy databases
            try
            {
                using var checkPoints = new MySqlCommand("SHOW COLUMNS FROM Customers LIKE 'Points';", connection);
                var pointsExists = checkPoints.ExecuteScalar();
                if (pointsExists == null)
                {
                    using var addPoints = new MySqlCommand("ALTER TABLE Customers ADD COLUMN Points INT NOT NULL DEFAULT 0;", connection);
                    addPoints.ExecuteNonQuery();
                }
            }
            catch { }

            // Invoices table
            string invoicesCmd = @"CREATE TABLE IF NOT EXISTS Invoices (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                CustomerId INT NOT NULL,
                EmployeeId INT NOT NULL,
                Subtotal DECIMAL(12,2) NOT NULL,
                TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
                TaxAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
                Discount DECIMAL(12,2) NOT NULL DEFAULT 0,
                Total DECIMAL(12,2) NOT NULL,
                Paid DECIMAL(12,2) NOT NULL DEFAULT 0,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id)
            );";
            using var invCmd = new MySqlCommand(invoicesCmd, connection);
            invCmd.ExecuteNonQuery();

            // InvoiceItems table
            string invoiceItemsCmd = @"CREATE TABLE IF NOT EXISTS InvoiceItems (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                InvoiceId INT NOT NULL,
                ProductId INT NOT NULL,
                EmployeeId INT NOT NULL,
                UnitPrice DECIMAL(12,2) NOT NULL,
                Quantity INT NOT NULL,
                LineTotal DECIMAL(12,2) NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
                FOREIGN KEY (ProductId) REFERENCES Products(Id),
                FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id)
            );";
            using var invItemsCmd = new MySqlCommand(invoiceItemsCmd, connection);
            invItemsCmd.ExecuteNonQuery();

            // Update existing Products table if needed
            UpdateProductsTable(connection);


            // Fix any existing data issues
            FixExistingProductData(connection);

            // Default admin account (already present)
            string checkAdminCmd = "SELECT COUNT(*) FROM Accounts WHERE Username='admin';";
            using var checkCmd = new MySqlCommand(checkAdminCmd, connection);
            long adminExists = (long)checkCmd.ExecuteScalar();
            if (adminExists == 0)
            {
                string insertAdminCmd = "INSERT INTO Accounts (Username, Password, Role) VALUES ('admin', 'admin', 'Admin');";
                using var insertCmd = new MySqlCommand(insertAdminCmd, connection);
                insertCmd.ExecuteNonQuery();
            }
        }

        private static void UpdateProductsTable(MySqlConnection connection)
        {
            try
            {
                // Check if Code column exists
                string checkCodeCmd = "SHOW COLUMNS FROM Products LIKE 'Code';";
                using var checkCode = new MySqlCommand(checkCodeCmd, connection);
                var codeExists = checkCode.ExecuteScalar();

                if (codeExists == null)
                {
                    // Add Code column without UNIQUE constraint first
                    string addCodeCmd = "ALTER TABLE Products ADD COLUMN Code VARCHAR(50);";
                    using var addCode = new MySqlCommand(addCodeCmd, connection);
                    addCode.ExecuteNonQuery();

                    // Update existing records to have unique codes
                    string updateCodesCmd = "UPDATE Products SET Code = CONCAT('PROD', LPAD(Id, 4, '0')) WHERE Code IS NULL OR Code = '';";
                    using var updateCodes = new MySqlCommand(updateCodesCmd, connection);
                    updateCodes.ExecuteNonQuery();

                    // Now add UNIQUE constraint
                    string addUniqueCmd = "ALTER TABLE Products ADD UNIQUE (Code);";
                    using var addUnique = new MySqlCommand(addUniqueCmd, connection);
                    addUnique.ExecuteNonQuery();
                }

                // Check if Description column exists
                string checkDescCmd = "SHOW COLUMNS FROM Products LIKE 'Description';";
                using var checkDesc = new MySqlCommand(checkDescCmd, connection);
                var descExists = checkDesc.ExecuteScalar();

                if (descExists == null)
                {
                    // Add Description column
                    string addDescCmd = "ALTER TABLE Products ADD COLUMN Description TEXT;";
                    using var addDesc = new MySqlCommand(addDescCmd, connection);
                    addDesc.ExecuteNonQuery();
                }

                // Check if CreatedDate column exists
                string checkCreatedCmd = "SHOW COLUMNS FROM Products LIKE 'CreatedDate';";
                using var checkCreated = new MySqlCommand(checkCreatedCmd, connection);
                var createdExists = checkCreated.ExecuteScalar();

                if (createdExists == null)
                {
                    // Add CreatedDate column
                    string addCreatedCmd = "ALTER TABLE Products ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP;";
                    using var addCreated = new MySqlCommand(addCreatedCmd, connection);
                    addCreated.ExecuteNonQuery();
                }

                // Check if UpdatedDate column exists
                string checkUpdatedCmd = "SHOW COLUMNS FROM Products LIKE 'UpdatedDate';";
                using var checkUpdated = new MySqlCommand(checkUpdatedCmd, connection);
                var updatedExists = checkUpdated.ExecuteScalar();

                if (updatedExists == null)
                {
                    // Add UpdatedDate column
                    string addUpdatedCmd = "ALTER TABLE Products ADD COLUMN UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;";
                    using var addUpdated = new MySqlCommand(addUpdatedCmd, connection);
                    addUpdated.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error updating Products table: {ex.Message}");
            }
        }

        private static void FixExistingProductData(MySqlConnection connection)
        {
            try
            {
                // Check if there are any NULL or empty codes and fix them
                string fixCodesCmd = "UPDATE Products SET Code = CONCAT('PROD', LPAD(Id, 4, '0')) WHERE Code IS NULL OR Code = '';";
                using var fixCodes = new MySqlCommand(fixCodesCmd, connection);
                fixCodes.ExecuteNonQuery();

                // Check for duplicate codes and fix them
                string checkDuplicatesCmd = @"
                    UPDATE Products p1 
                    SET Code = CONCAT('PROD', LPAD(p1.Id, 4, '0'), '_', FLOOR(RAND() * 1000))
                    WHERE EXISTS (
                        SELECT 1 FROM Products p2 
                        WHERE p2.Code = p1.Code AND p2.Id != p1.Id
                    );";
                using var fixDuplicates = new MySqlCommand(checkDuplicatesCmd, connection);
                fixDuplicates.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error fixing existing product data: {ex.Message}");
            }
        }

        public static bool RegisterAccount(string username, string employeeName, string password, string role = "Cashier")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Accounts (Username, EmployeeName, Password, Role) VALUES (@username, @employeeName, @password, @role);";
            using var cmd = new MySqlCommand(insertCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@employeeName", employeeName);
            cmd.Parameters.AddWithValue("@password", password);
            cmd.Parameters.AddWithValue("@role", role);
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RegisterAccount(string username, string password, string role = "Cashier")
        {
            // Overload method để tương thích ngược - sử dụng username làm EmployeeName
            return RegisterAccount(username, username, password, role);
        }

        public static string ValidateLogin(string username, string password)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@password;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);
            long count = (long)cmd.ExecuteScalar();
            return count > 0 ? "true" : "false";
        }

        public static string GetUserRole(string username)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Role FROM Accounts WHERE Username=@username;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "Cashier";
        }

        public static UserRole GetUserRoleEnum(string username)
        {
            string roleString = GetUserRole(username);
            return roleString.ToLower() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                "cashier" => UserRole.Cashier,
                _ => UserRole.Cashier
            };
        }

        public static bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Verify the old password
            string verifyCmd = "SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@oldPassword;";
            using var verify = new MySqlCommand(verifyCmd, connection);
            verify.Parameters.AddWithValue("@username", username);
            verify.Parameters.AddWithValue("@oldPassword", oldPassword);
            long count = (long)verify.ExecuteScalar();

            if (count == 0)
                return false; // Old password incorrect

            // Update to the new password
            string updateCmd = "UPDATE Accounts SET Password=@newPassword WHERE Username=@username;";
            using var update = new MySqlCommand(updateCmd, connection);
            update.Parameters.AddWithValue("@username", username);
            update.Parameters.AddWithValue("@newPassword", newPassword);
            return update.ExecuteNonQuery() > 0;
        }

        public static List<(int Id, string Username)> GetAllAccounts()
        {
            var accounts = new List<(int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Username FROM Accounts;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                accounts.Add((reader.GetInt32(0), reader.GetString(1)));
            }
            return accounts;
        }

        public static int GetEmployeeIdByUsername(string username)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id FROM Accounts WHERE Username = @username;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 1; // Default to admin ID
        }

        public static bool DeleteAccount(string username)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string deleteCmd = "DELETE FROM Accounts WHERE Username=@username;";
            using var cmd = new MySqlCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            return cmd.ExecuteNonQuery() > 0;
        }


        public static bool AddProduct(string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int stockQuantity, string description = "")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Kiểm tra xem bảng Products có SalePrice hay Price
            string checkColumnCmd = "SHOW COLUMNS FROM Products LIKE 'SalePrice';";
            using var checkColumn = new MySqlCommand(checkColumnCmd, connection);
            var salePriceExists = checkColumn.ExecuteScalar();

            string cmdText;
            if (salePriceExists != null)
            {
                // Sử dụng SalePrice, PurchasePrice, PurchaseUnit
                cmdText = "INSERT INTO Products (Name, Code, CategoryId, SalePrice, PurchasePrice, PurchaseUnit, StockQuantity, Description) VALUES (@name, @code, @categoryId, @salePrice, @purchasePrice, @purchaseUnit, @stockQuantity, @description);";
                using var cmd = new MySqlCommand(cmdText, connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@salePrice", salePrice);
                cmd.Parameters.AddWithValue("@purchasePrice", purchasePrice);
                cmd.Parameters.AddWithValue("@purchaseUnit", purchaseUnit);
                cmd.Parameters.AddWithValue("@stockQuantity", stockQuantity);
                cmd.Parameters.AddWithValue("@description", description);
                try
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // Fallback cho cấu trúc cũ
                cmdText = "INSERT INTO Products (Name, Code, CategoryId, Price, StockQuantity, Description) VALUES (@name, @code, @categoryId, @price, @stockQuantity, @description);";
                using var cmd = new MySqlCommand(cmdText, connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@price", salePrice); // Sử dụng salePrice làm Price trong cấu trúc cũ
                cmd.Parameters.AddWithValue("@stockQuantity", stockQuantity);
                cmd.Parameters.AddWithValue("@description", description);
                try
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool AddProduct(string name, string code, int categoryId, decimal price, int stockQuantity, string description = "")
        {
            // Overload method để tương thích ngược - giả định purchasePrice = price * 0.8
            return AddProduct(name, code, categoryId, price, Math.Round(price * 0.8m, 2), "Piece", stockQuantity, description);
        }

        /// <summary>
        /// Debug method to test product addition with detailed error reporting
        /// </summary>
        public static string TestAddProduct(string name, string code, int categoryId, decimal price, int stockQuantity, string description = "")
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine($"=== TEST THÊM SẢN PHẨM: {name} ===");

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                result.AppendLine("✅ Kết nối database thành công");

                // Kiểm tra categoryId có tồn tại không
                string checkCategoryCmd = "SELECT COUNT(*) FROM Categories WHERE Id = @categoryId;";
                using var checkCat = new MySqlCommand(checkCategoryCmd, connection);
                checkCat.Parameters.AddWithValue("@categoryId", categoryId);
                int categoryExists = Convert.ToInt32(checkCat.ExecuteScalar());

                if (categoryExists == 0)
                {
                    result.AppendLine($"❌ Danh mục ID {categoryId} không tồn tại!");

                    // Hiển thị danh mục có sẵn
                    string listCategoriesCmd = "SELECT Id, Name FROM Categories ORDER BY Id;";
                    using var listCat = new MySqlCommand(listCategoriesCmd, connection);
                    using var reader = listCat.ExecuteReader();
                    result.AppendLine("📋 Danh mục có sẵn:");
                    while (reader.Read())
                    {
                        result.AppendLine($"  - ID: {reader["Id"]}, Tên: {reader["Name"]}");
                    }
                    reader.Close();
                    return result.ToString();
                }
                result.AppendLine($"✅ Danh mục ID {categoryId} tồn tại");

                // Kiểm tra code có trùng không
                string checkCodeCmd = "SELECT COUNT(*) FROM Products WHERE Code = @code;";
                using var checkCode = new MySqlCommand(checkCodeCmd, connection);
                checkCode.Parameters.AddWithValue("@code", code);
                int codeExists = Convert.ToInt32(checkCode.ExecuteScalar());

                if (codeExists > 0)
                {
                    result.AppendLine($"❌ Mã sản phẩm '{code}' đã tồn tại!");
                    return result.ToString();
                }
                result.AppendLine($"✅ Mã sản phẩm '{code}' chưa tồn tại");

                // Thử thêm sản phẩm
                string insertCmd = "INSERT INTO Products (Name, Code, CategoryId, SalePrice, StockQuantity, Description) VALUES (@name, @code, @categoryId, @price, @stockQuantity, @description);";
                using var cmd = new MySqlCommand(insertCmd, connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@saleprice", price);
                cmd.Parameters.AddWithValue("@stockQuantity", stockQuantity);
                cmd.Parameters.AddWithValue("@description", description);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    result.AppendLine($"✅ Thêm sản phẩm '{name}' thành công!");
                }
                else
                {
                    result.AppendLine($"❌ Không thể thêm sản phẩm '{name}'");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ Lỗi: {ex.Message}");
            }

            return result.ToString();
        }

        public static List<(int Id, string Name, string Code, int CategoryId, decimal SalePrice, int StockQuantity, string Description)> GetAllProducts()
        {
            var products = new List<(int, string, string, int, decimal, int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Kiểm tra xem bảng Products có SalePrice hay Price
            string checkColumnCmd = "SHOW COLUMNS FROM Products LIKE 'SalePrice';";
            using var checkColumn = new MySqlCommand(checkColumnCmd, connection);
            var salePriceExists = checkColumn.ExecuteScalar();

            string cmdText;
            if (salePriceExists != null)
            {
                cmdText = "SELECT Id, Name, Code, CategoryId, SalePrice, StockQuantity, Description FROM Products ORDER BY Name;";
            }
            else
            {
                cmdText = "SELECT Id, Name, Code, CategoryId, Price, StockQuantity, Description FROM Products ORDER BY Name;";
            }

            using var cmd = new MySqlCommand(cmdText, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    reader.GetDecimal(4),
                    reader.GetInt32(5),
                    reader.IsDBNull(6) ? "" : reader.GetString(6)
                ));
            }
            return products;
        }

        public static List<(int Id, string Name, string Code, int CategoryId, string CategoryName, decimal Price, int StockQuantity, string Description)> GetAllProductsWithCategories()
        {
            var products = new List<(int, string, string, int, string, decimal, int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string cmdText = @"SELECT p.Id, p.Name, p.Code, p.CategoryId, c.Name as CategoryName, p.SalePrice, p.StockQuantity, p.Description 
                              FROM Products p 
                              LEFT JOIN Categories c ON p.CategoryId = c.Id 
                              ORDER BY p.Name;";
            using var cmd = new MySqlCommand(cmdText, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    reader.IsDBNull(4) ? "Uncategorized" : reader.GetString(4),
                    reader.GetDecimal(5),
                    reader.GetInt32(6),
                    reader.IsDBNull(7) ? "" : reader.GetString(7)
                ));
            }
            return products;
        }

        public static bool UpdateProduct(int id, string name, string code, int categoryId, decimal price, int stockQuantity, string description = "")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string cmdText = "UPDATE Products SET Name=@name, Code=@code, CategoryId=@categoryId, salePrice=@saleprice, StockQuantity=@stockQuantity, Description=@description WHERE Id=@id;";
            using var cmd = new MySqlCommand(cmdText, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@saleprice", price);
            cmd.Parameters.AddWithValue("@stockQuantity", stockQuantity);
            cmd.Parameters.AddWithValue("@description", description);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false; // Code already exists or other error
            }
        }

        public static bool DeleteProduct(int id)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if product is used by any invoices (when we implement them)
            try
            {
                string checkCmd = "SELECT COUNT(*) FROM InvoiceItems WHERE ProductId=@id;";
                using var check = new MySqlCommand(checkCmd, connection);
                check.Parameters.AddWithValue("@id", id);
                long count = (long)check.ExecuteScalar();

                if (count > 0)
                {
                    return false; // Product is in use by invoices
                }
            }
            catch
            {
                // InvoiceItems table doesn't exist yet, so we can safely delete
            }

            try
            {
                // First, try to delete normally
                string cmdText = "DELETE FROM Products WHERE Id=@id;";
                using var cmd = new MySqlCommand(cmdText, connection);
                cmd.Parameters.AddWithValue("@id", id);
                int result = cmd.ExecuteNonQuery();
                return result > 0;
            }
            catch (Exception ex)
            {
                // If normal delete fails, try with foreign key checks disabled
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Normal delete failed: {ex.Message}, trying with FK checks disabled");

                    // Disable foreign key checks temporarily
                    using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection);
                    disableFK.ExecuteNonQuery();

                    // Try delete again
                    string cmdText = "DELETE FROM Products WHERE Id=@id;";
                    using var cmd = new MySqlCommand(cmdText, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    int result = cmd.ExecuteNonQuery();

                    // Re-enable foreign key checks
                    using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection);
                    enableFK.ExecuteNonQuery();

                    return result > 0;
                }
                catch (Exception ex2)
                {
                    // Log the specific error for debugging
                    System.Diagnostics.Debug.WriteLine($"Error deleting product with FK checks disabled: {ex2.Message}");
                    return false;
                }
            }
        }

        public static bool DeleteAllProducts()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            try
            {
                // Check if any products are used by invoices
                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM InvoiceItems;";
                    using var check = new MySqlCommand(checkCmd, connection);
                    long count = (long)check.ExecuteScalar();

                    if (count > 0)
                    {
                        return false; // Some products are in use by invoices
                    }
                }
                catch
                {
                    // InvoiceItems table doesn't exist yet, so we can safely delete all
                }

                // First, try to delete normally
                string cmdText = "DELETE FROM Products;";
                using var cmd = new MySqlCommand(cmdText, connection);
                int result = cmd.ExecuteNonQuery();
                return true; // Return true even if no products were deleted
            }
            catch (Exception ex)
            {
                // If normal delete fails, try with foreign key checks disabled
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Normal delete all failed: {ex.Message}, trying with FK checks disabled");

                    // Disable foreign key checks temporarily
                    using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection);
                    disableFK.ExecuteNonQuery();

                    // Try delete again
                    string cmdText = "DELETE FROM Products;";
                    using var cmd = new MySqlCommand(cmdText, connection);
                    int result = cmd.ExecuteNonQuery();

                    // Re-enable foreign key checks
                    using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection);
                    enableFK.ExecuteNonQuery();

                    return true;
                }
                catch (Exception ex2)
                {
                    // Log the specific error for debugging
                    System.Diagnostics.Debug.WriteLine($"Error deleting all products with FK checks disabled: {ex2.Message}");
                    return false;
                }
            }
        }

        // Invoice persistence
        public static bool SaveInvoice(
            int customerId,
            int employeeId,
            decimal subtotal,
            decimal taxPercent,
            decimal taxAmount,
            decimal discount,
            decimal total,
            decimal paid,
            List<(int ProductId, int Quantity, decimal UnitPrice)> items)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Insert invoice
                string insertInvoice = @"INSERT INTO Invoices (CustomerId, EmployeeId, Subtotal, TaxPercent, TaxAmount, Discount, Total, Paid)
                                         VALUES (@CustomerId, @EmployeeId, @Subtotal, @TaxPercent, @TaxAmount, @Discount, @Total, @Paid);
                                         SELECT LAST_INSERT_ID();";
                using var invCmd = new MySqlCommand(insertInvoice, connection, tx);
                invCmd.Parameters.AddWithValue("@CustomerId", customerId);
                invCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                invCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                invCmd.Parameters.AddWithValue("@TaxPercent", taxPercent);
                invCmd.Parameters.AddWithValue("@TaxAmount", taxAmount);
                invCmd.Parameters.AddWithValue("@Discount", discount);
                invCmd.Parameters.AddWithValue("@Total", total);
                invCmd.Parameters.AddWithValue("@Paid", paid);
                var invoiceIdObj = invCmd.ExecuteScalar();
                int invoiceId = Convert.ToInt32(invoiceIdObj);

                // Insert items and update stock
                foreach (var (productId, quantity, unitPrice) in items)
                {
                    decimal lineTotal = unitPrice * quantity;
                    string insertItem = @"INSERT INTO InvoiceItems (InvoiceId, ProductId, EmployeeId, UnitPrice, Quantity, LineTotal)
                                           VALUES (@InvoiceId, @ProductId, @EmployeeId, @UnitPrice, @Quantity, @LineTotal);";
                    using var itemCmd = new MySqlCommand(insertItem, connection, tx);
                    itemCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    itemCmd.Parameters.AddWithValue("@ProductId", productId);
                    itemCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    itemCmd.Parameters.AddWithValue("@Quantity", quantity);
                    itemCmd.Parameters.AddWithValue("@LineTotal", lineTotal);
                    itemCmd.ExecuteNonQuery();

                    // Decrease stock
                    string updateStock = "UPDATE Products SET StockQuantity = GREATEST(0, StockQuantity - @qty) WHERE Id=@pid;";
                    using var stockCmd = new MySqlCommand(updateStock, connection, tx);
                    stockCmd.Parameters.AddWithValue("@qty", quantity);
                    stockCmd.Parameters.AddWithValue("@pid", productId);
                    stockCmd.ExecuteNonQuery();
                }

                tx.Commit();
                // Store the last invoice id in a session-like holder for UI feedback
                LastSavedInvoiceId = invoiceId;
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error saving invoice: {ex.Message}");
                return false;
            }
        }

        // Exposed last saved invoice id for quick feedback after SaveInvoice
        public static int LastSavedInvoiceId { get; private set; }

        // Customer management methods
        public static List<(int Id, string Name, string Phone, string Email, string Address, string CustomerType)> GetAllCustomers()
        {
            var customers = new List<(int, string, string, string, string, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Name, Phone, Email, Address, CustomerType FROM Customers ORDER BY Name;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                customers.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    reader.IsDBNull(5) ? "Regular" : reader.GetString(5)
                ));
            }
            return customers;
        }

        // Loyalty helpers
        public static (string Tier, int Points) GetCustomerLoyalty(int customerId)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT IFNULL(CustomerType,'Regular'), IFNULL(Points,0) FROM Customers WHERE Id=@id;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", customerId);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return (r.IsDBNull(0) ? "Regular" : r.GetString(0), r.IsDBNull(1) ? 0 : r.GetInt32(1));
            }
            return ("Regular", 0);
        }

        public static bool UpdateCustomerLoyalty(int customerId, int newPoints, string newTier)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = "UPDATE Customers SET Points=@p, CustomerType=@tier WHERE Id=@id;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@p", newPoints);
            cmd.Parameters.AddWithValue("@tier", newTier);
            cmd.Parameters.AddWithValue("@id", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // KPI helpers for dashboard
        public static decimal GetRevenueBetween(DateTime from, DateTime to)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT IFNULL(SUM(Total), 0) FROM Invoices WHERE CreatedDate BETWEEN @from AND @to";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            var val = cmd.ExecuteScalar();
            return Convert.ToDecimal(val ?? 0);
        }

        public static int GetInvoiceCountBetween(DateTime from, DateTime to)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT COUNT(*) FROM Invoices WHERE CreatedDate BETWEEN @from AND @to";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static int GetTotalCustomers()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Customers", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static int GetTotalProducts()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Products", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        // Reporting queries
        public static List<(int Id, DateTime CreatedDate, string CustomerName, decimal Subtotal, decimal TaxAmount, decimal Discount, decimal Total, decimal Paid)>
            QueryInvoices(DateTime? from, DateTime? to, int? customerId, string search)
        {
            var list = new List<(int, DateTime, string, decimal, decimal, decimal, decimal, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var sb = new System.Text.StringBuilder();
            sb.Append(@"SELECT i.Id, i.CreatedDate, c.Name, i.Subtotal, i.TaxAmount, i.Discount, i.Total, i.Paid
                       FROM Invoices i
                       LEFT JOIN Customers c ON c.Id = i.CustomerId
                       WHERE 1=1");

            if (from.HasValue) sb.Append(" AND i.CreatedDate >= @from");
            if (to.HasValue) sb.Append(" AND i.CreatedDate <= @to");
            if (customerId.HasValue) sb.Append(" AND i.CustomerId = @cust");
            if (!string.IsNullOrWhiteSpace(search)) sb.Append(" AND (c.Name LIKE @q OR i.Id LIKE @q)");
            sb.Append(" ORDER BY i.CreatedDate DESC, i.Id DESC");

            using var cmd = new MySqlCommand(sb.ToString(), connection);
            if (from.HasValue) cmd.Parameters.AddWithValue("@from", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("@to", to.Value);
            if (customerId.HasValue) cmd.Parameters.AddWithValue("@cust", customerId.Value);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@q", "%" + search + "%");

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((
                    r.GetInt32(0),
                    r.GetDateTime(1),
                    r.IsDBNull(2) ? "" : r.GetString(2),
                    r.GetDecimal(3),
                    r.GetDecimal(4),
                    r.GetDecimal(5),
                    r.GetDecimal(6),
                    r.GetDecimal(7)
                ));
            }
            return list;
        }

        public static (InvoiceHeader Header, List<InvoiceItemDetail> Items) GetInvoiceDetails(int invoiceId)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Header
            string headerSql = @"SELECT i.Id, i.CreatedDate, c.Name, i.Subtotal, i.TaxAmount, i.Discount, i.Total, i.Paid
                                 FROM Invoices i
                                 LEFT JOIN Customers c ON c.Id = i.CustomerId
                                 WHERE i.Id = @id";
            using var hcmd = new MySqlCommand(headerSql, connection);
            hcmd.Parameters.AddWithValue("@id", invoiceId);
            using var hr = hcmd.ExecuteReader();
            InvoiceHeader header;
            if (hr.Read())
            {
                header = new InvoiceHeader
                {
                    Id = hr.GetInt32(0),
                    CreatedDate = hr.GetDateTime(1),
                    CustomerName = hr.IsDBNull(2) ? "" : hr.GetString(2),
                    Subtotal = hr.GetDecimal(3),
                    TaxAmount = hr.GetDecimal(4),
                    Discount = hr.GetDecimal(5),
                    Total = hr.GetDecimal(6),
                    Paid = hr.GetDecimal(7)
                };
            }
            else
            {
                return (new InvoiceHeader { Id = invoiceId }, new List<InvoiceItemDetail>());
            }
            hr.Close();

            // Items
            var items = new List<InvoiceItemDetail>();
            string itemsSql = @"SELECT ii.ProductId, p.Name, ii.UnitPrice, ii.Quantity, ii.LineTotal
                                 FROM InvoiceItems ii
                                 LEFT JOIN Products p ON p.Id = ii.ProductId
                                 WHERE ii.InvoiceId = @id";
            using var icmd = new MySqlCommand(itemsSql, connection);
            icmd.Parameters.AddWithValue("@id", invoiceId);
            using var ir = icmd.ExecuteReader();
            while (ir.Read())
            {
                items.Add(new InvoiceItemDetail
                {
                    ProductId = ir.GetInt32(0),
                    ProductName = ir.IsDBNull(1) ? "" : ir.GetString(1),
                    UnitPrice = ir.GetDecimal(2),
                    Quantity = ir.GetInt32(3),
                    LineTotal = ir.GetDecimal(4)
                });
            }

            return (header, items);
        }

        public class InvoiceHeader
        {
            public int Id { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal Subtotal { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal Discount { get; set; }
            public decimal Total { get; set; }
            public decimal Paid { get; set; }
        }

        public class InvoiceItemDetail
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }

        public static bool DeleteInvoice(int invoiceId)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Delete invoice; InvoiceItems has ON DELETE CASCADE for InvoiceId
                using var del = new MySqlCommand("DELETE FROM Invoices WHERE Id=@id;", connection, tx);
                del.Parameters.AddWithValue("@id", invoiceId);
                int affected = del.ExecuteNonQuery();

                tx.Commit();
                return affected > 0;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error deleting invoice {invoiceId}: {ex.Message}");
                return false;
            }
        }

        // Aggregations for charts
        public static List<(DateTime Day, decimal Revenue)> GetRevenueByDay(DateTime from, DateTime to)
        {
            var list = new List<(DateTime, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT DATE(CreatedDate) as d, SUM(Total) as revenue
                           FROM Invoices
                           WHERE CreatedDate BETWEEN @from AND @to
                           GROUP BY DATE(CreatedDate)
                           ORDER BY d";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((r.GetDateTime(0), r.IsDBNull(1) ? 0m : r.GetDecimal(1)));
            }
            return list;
        }

        public static List<(string ProductName, int Quantity)> GetTopProducts(DateTime from, DateTime to, int topN = 10)
        {
            var list = new List<(string, int)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT p.Name, SUM(ii.Quantity) as qty
                           FROM InvoiceItems ii
                           JOIN Invoices i ON i.Id = ii.InvoiceId
                           LEFT JOIN Products p ON p.Id = ii.ProductId
                           WHERE i.CreatedDate BETWEEN @from AND @to
                           GROUP BY p.Name
                           ORDER BY qty DESC
                           LIMIT @top";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            cmd.Parameters.AddWithValue("@top", topN);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((r.IsDBNull(0) ? "(Unknown)" : r.GetString(0), r.IsDBNull(1) ? 0 : r.GetInt32(1)));
            }
            return list;
        }

        public static List<(string CategoryName, decimal Revenue)> GetRevenueByCategory(DateTime from, DateTime to, int topN = 8)
        {
            var list = new List<(string, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT IFNULL(c.Name, 'Uncategorized') as CategoryName, SUM(ii.LineTotal) as Revenue
                           FROM InvoiceItems ii
                           JOIN Invoices i ON i.Id = ii.InvoiceId
                           LEFT JOIN Products p ON p.Id = ii.ProductId
                           LEFT JOIN Categories c ON c.Id = p.CategoryId
                           WHERE i.CreatedDate BETWEEN @from AND @to
                           GROUP BY IFNULL(c.Name, 'Uncategorized')
                           ORDER BY Revenue DESC
                           LIMIT @top";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            cmd.Parameters.AddWithValue("@top", topN);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((r.IsDBNull(0) ? "Uncategorized" : r.GetString(0), r.IsDBNull(1) ? 0m : r.GetDecimal(1)));
            }
            return list;
        }

        public static bool AddCustomer(string name, string phone, string email, string customerType, string address)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Customers (Name, Phone, Email, CustomerType, Address) VALUES (@name, @phone, @email, @customerType, @address);";
            using var cmd = new MySqlCommand(insertCmd, connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@customerType", customerType);
            cmd.Parameters.AddWithValue("@address", address);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateCustomer(int id, string name, string phone, string email, string customerType, string address)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string updateCmd = "UPDATE Customers SET Name=@name, Phone=@phone, Email=@email, CustomerType=@customerType, Address=@address WHERE Id=@id;";
            using var cmd = new MySqlCommand(updateCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@customerType", customerType);
            cmd.Parameters.AddWithValue("@address", address);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteCustomer(int id)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if customer is used by any invoices (when we implement them)
            try
            {
                string checkCmd = "SELECT COUNT(*) FROM Invoices WHERE CustomerId=@id;";
                using var check = new MySqlCommand(checkCmd, connection);
                check.Parameters.AddWithValue("@id", id);
                long count = (long)check.ExecuteScalar();

                if (count > 0)
                {
                    return false; // Customer is in use by invoices
                }
            }
            catch
            {
                // Invoices table doesn't exist yet, so we can safely delete
            }

            string deleteCmd = "DELETE FROM Customers WHERE Id=@id;";
            using var cmd = new MySqlCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        // Category management methods
        public static List<(int Id, string Name)> GetAllCategories()
        {
            var categories = new List<(int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Name FROM Categories ORDER BY Name;";
            using var cmd = new MySqlCommand(selectCmd, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add((reader.GetInt32(0), reader.GetString(1)));
            }
            return categories;
        }

        public static bool AddCategory(string name)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Categories (Name) VALUES (@name);";
            using var cmd = new MySqlCommand(insertCmd, connection);
            cmd.Parameters.AddWithValue("@name", name);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false; // Category already exists or other error
            }
        }

        public static bool UpdateCategory(int id, string name)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string updateCmd = "UPDATE Categories SET Name=@name WHERE Id=@id;";
            using var cmd = new MySqlCommand(updateCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false; // Category name already exists or other error
            }
        }

        public static bool DeleteCategory(int id)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if category is used by any products
            string checkCmd = "SELECT COUNT(*) FROM Products WHERE CategoryId=@id;";
            using var check = new MySqlCommand(checkCmd, connection);
            check.Parameters.AddWithValue("@id", id);
            long count = (long)check.ExecuteScalar();

            if (count > 0)
            {
                return false; // Category is in use
            }

            string deleteCmd = "DELETE FROM Categories WHERE Id=@id;";
            using var cmd = new MySqlCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }




        /// <summary>
        /// Tạo mã sản phẩm duy nhất
        /// </summary>
        private static string GenerateUniqueProductCode(string baseCode, MySqlConnection connection)
        {
            int counter = 1;
            string code = baseCode;

            while (true)
            {
                string checkCmd = "SELECT COUNT(*) FROM Products WHERE Code = @code;";
                using var check = new MySqlCommand(checkCmd, connection);
                check.Parameters.AddWithValue("@code", code);
                int exists = Convert.ToInt32(check.ExecuteScalar());

                if (exists == 0) return code;

                counter++;
                code = $"{baseCode}{counter:D3}";
            }
        }


        public static List<(int InvoiceId, DateTime CreatedAt, int ItemCount, decimal Total)> GetCustomerPurchaseHistory(int customerId)
        {
            var list = new List<(int, DateTime, int, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

            string sql = @"
                SELECT i.Id, IFNULL(i.CreatedDate, NOW()) AS CreatedAt,
                       (SELECT COUNT(*) FROM InvoiceItems ii WHERE ii.InvoiceId = i.Id) AS ItemCount,
                       i.Total
                FROM Invoices i
                WHERE i.CustomerId = @cid
                ORDER BY i.CreatedDate DESC, i.Id DESC;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@cid", customerId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((
                    r.GetInt32(0),
                    r.IsDBNull(1) ? DateTime.Now : r.GetDateTime(1),
                    r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2)),
                    r.IsDBNull(3) ? 0m : r.GetDecimal(3)
                ));
            }
            return list;
        }

        // Segment khách hàng
        public static string GetCustomerSegment(int id)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

                using (var check = new MySqlCommand("SHOW COLUMNS FROM Customers LIKE 'Segment';", connection))
                {
                    var ok = check.ExecuteScalar();
                    if (ok == null) return string.Empty;
                }

                using var cmd = new MySqlCommand("SELECT Segment FROM Customers WHERE Id=@id;", connection);
                cmd.Parameters.AddWithValue("@id", id);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool UpdateCustomerSegment(int id, string? segment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(segment)) return true;
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

                using (var check = new MySqlCommand("SHOW COLUMNS FROM Customers LIKE 'Segment';", connection))
                {
                    var ok = check.ExecuteScalar();
                    if (ok == null) return true;
                }

                using var cmd = new MySqlCommand("UPDATE Customers SET Segment=@segment WHERE Id=@id;", connection);
                cmd.Parameters.AddWithValue("@segment", segment);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        // Chi tiết hóa đơn
        public static List<(string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)> GetInvoiceItemsDetailed(int invoiceId)
        {
            var list = new List<(string, int, decimal, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

            string sql = @"
                SELECT p.Name AS ProductName, ii.Quantity, ii.UnitPrice, (ii.UnitPrice * ii.Quantity) AS LineTotal
                FROM InvoiceItems ii
                LEFT JOIN Products p ON p.Id = ii.ProductId
                WHERE ii.InvoiceId = @invoiceId;";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@invoiceId", invoiceId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((
                    r.IsDBNull(0) ? string.Empty : r.GetString(0),
                    r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    r.IsDBNull(2) ? 0m : r.GetDecimal(2),
                    r.IsDBNull(3) ? 0m : r.GetDecimal(3)
                ));
            }
            return list;
        }

        // Cập nhật tài khoản
        public static bool UpdateAccount(string username, string? newPassword = null, string? newRole = null, string? newEmployeeName = null)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

            var sets = new List<string>();
            using var cmd = new MySqlCommand();
            cmd.Connection = connection;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                sets.Add("Password=@password");
                cmd.Parameters.AddWithValue("@password", newPassword);
            }
            if (!string.IsNullOrWhiteSpace(newRole))
            {
                sets.Add("Role=@role");
                cmd.Parameters.AddWithValue("@role", newRole);
            }
            if (!string.IsNullOrWhiteSpace(newEmployeeName))
            {
                sets.Add("EmployeeName=@employeeName");
                cmd.Parameters.AddWithValue("@employeeName", newEmployeeName);
            }

            if (sets.Count == 0) return true;

            cmd.CommandText = $"UPDATE Accounts SET {string.Join(", ", sets)} WHERE Username=@username;";
            cmd.Parameters.AddWithValue("@username", username);

            try { return cmd.ExecuteNonQuery() > 0; }
            catch (MySqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateAccount error: {ex.Message}");
                return false;
            }
        }

    }
}