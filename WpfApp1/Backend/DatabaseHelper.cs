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
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            );";
            using var custCmd = new MySqlCommand(customerCmd, connection);
            custCmd.ExecuteNonQuery();

            // Invoices table
            string invoicesCmd = @"CREATE TABLE IF NOT EXISTS Invoices (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                CustomerId INT NOT NULL,
                Subtotal DECIMAL(12,2) NOT NULL,
                TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
                TaxAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
                Discount DECIMAL(12,2) NOT NULL DEFAULT 0,
                Total DECIMAL(12,2) NOT NULL,
                Paid DECIMAL(12,2) NOT NULL DEFAULT 0,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
            );";
            using var invCmd = new MySqlCommand(invoicesCmd, connection);
            invCmd.ExecuteNonQuery();

            // InvoiceItems table
            string invoiceItemsCmd = @"CREATE TABLE IF NOT EXISTS InvoiceItems (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                InvoiceId INT NOT NULL,
                ProductId INT NOT NULL,
                UnitPrice DECIMAL(12,2) NOT NULL,
                Quantity INT NOT NULL,
                LineTotal DECIMAL(12,2) NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
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

        public static bool UpdateUsername(string oldUsername, string newUsername)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string updateCmd = "UPDATE Accounts SET Username=@newUsername WHERE Username=@oldUsername;";
            using var cmd = new MySqlCommand(updateCmd, connection);
            cmd.Parameters.AddWithValue("@oldUsername", oldUsername);
            cmd.Parameters.AddWithValue("@newUsername", newUsername);
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

        public static void InitializeDefaultVietnameseData()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if data already exists - but allow creation if Products table is empty
            string checkProductsCmd = "SELECT COUNT(*) FROM Products;";
            using var checkProductsCmdObj = new MySqlCommand(checkProductsCmd, connection);
            long productCount = (long)checkProductsCmdObj.ExecuteScalar();
            
            if (productCount > 0)
            {
                // Products already exist, skip initialization
                return;
            }

            // Add default Vietnamese categories
            var defaultCategories = new[]
            {
                "Thực phẩm",
                "Đồ uống", 
                "Điện tử",
                "Quần áo",
                "Gia dụng",
                "Sách vở",
                "Thể thao",
                "Mỹ phẩm",
                "Đồ chơi",
                "Khác"
            };

            foreach (var category in defaultCategories)
            {
                try
                {
                    string insertCmd = "INSERT INTO Categories (Name) VALUES (@name);";
                    using var cmd = new MySqlCommand(insertCmd, connection);
                    cmd.Parameters.AddWithValue("@name", category);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    // Category already exists, ignore
                }
            }

            // Add default Vietnamese customers
            var defaultCustomers = new[]
            {
                ("Khách lẻ", "", "", "Thường", ""),
                ("Nguyễn Văn An", "0123456789", "an.nguyen@email.com", "VIP", "123 Đường ABC, Quận 1, TP.HCM"),
                ("Trần Thị Bình", "0987654321", "binh.tran@email.com", "Thường", "456 Đường XYZ, Quận 2, TP.HCM"),
                ("Lê Văn Cường", "0369258147", "cuong.le@email.com", "Sỉ", "789 Đường DEF, Quận 3, TP.HCM"),
                ("Phạm Thị Dung", "0741852963", "dung.pham@email.com", "Doanh nghiệp", "321 Đường GHI, Quận 4, TP.HCM")
            };

            foreach (var (name, phone, email, type, address) in defaultCustomers)
            {
                try
                {
                    string insertCmd = "INSERT IGNORE INTO Customers (Name, Phone, Email, CustomerType, Address) VALUES (@name, @phone, @email, @type, @address);";
                    using var cmd = new MySqlCommand(insertCmd, connection);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    // Customer already exists, ignore
                }
            }

            // Add default Vietnamese products - Extensive list
            var defaultProducts = new[]
            {
                // THỰC PHẨM (Category 1)
                ("Cơm tấm sườn nướng", "CT001", 1, 35000, 100, "Cơm tấm với sườn nướng thơm ngon"),
                ("Phở bò", "PB001", 1, 45000, 50, "Phở bò truyền thống"),
                ("Bánh mì thịt nướng", "BM001", 1, 15000, 200, "Bánh mì với thịt nướng"),
                ("Bún bò Huế", "BBH001", 1, 40000, 60, "Bún bò Huế cay nồng"),
                ("Bánh xèo", "BX001", 1, 30000, 80, "Bánh xèo tôm thịt"),
                ("Gỏi cuốn", "GC001", 1, 25000, 120, "Gỏi cuốn tôm thịt"),
                ("Chả cá Lã Vọng", "CCL001", 1, 55000, 40, "Chả cá Lã Vọng Hà Nội"),
                ("Bún chả", "BC001", 1, 35000, 70, "Bún chả Hà Nội"),
                ("Cơm cháy", "CC001", 1, 20000, 90, "Cơm cháy Ninh Bình"),
                ("Bánh tráng nướng", "BTN001", 1, 18000, 150, "Bánh tráng nướng Đà Lạt"),
                ("Nem nướng", "NN001", 1, 22000, 100, "Nem nướng Nha Trang"),
                ("Bánh canh", "BC001", 1, 28000, 80, "Bánh canh cua"),
                ("Hủ tiếu", "HT001", 1, 32000, 60, "Hủ tiếu Nam Vang"),
                ("Mì Quảng", "MQ001", 1, 38000, 50, "Mì Quảng đặc sản"),
                ("Bún riêu", "BR001", 1, 30000, 70, "Bún riêu cua"),
                ("Cháo lòng", "CL001", 1, 25000, 40, "Cháo lòng heo"),
                ("Bánh cuốn", "BC001", 1, 20000, 100, "Bánh cuốn nhân thịt"),
                ("Xôi gà", "XG001", 1, 15000, 120, "Xôi gà nướng"),
                ("Bánh ướt", "BU001", 1, 12000, 150, "Bánh ướt thịt nướng"),
                ("Cơm gà", "CG001", 1, 30000, 80, "Cơm gà Hải Nam"),

                // ĐỒ UỐNG (Category 2)
                ("Coca Cola", "CC001", 2, 12000, 300, "Nước ngọt Coca Cola 330ml"),
                ("Nước suối", "NS001", 2, 5000, 500, "Nước suối tinh khiết 500ml"),
                ("Trà sữa", "TS001", 2, 25000, 80, "Trà sữa trân châu"),
                ("Cà phê đen", "CFD001", 2, 15000, 200, "Cà phê đen phin"),
                ("Cà phê sữa", "CFS001", 2, 18000, 200, "Cà phê sữa đá"),
                ("Nước cam", "NC001", 2, 20000, 150, "Nước cam tươi"),
                ("Nước dừa", "ND001", 2, 15000, 100, "Nước dừa tươi"),
                ("Sinh tố bơ", "STB001", 2, 30000, 60, "Sinh tố bơ sữa"),
                ("Sinh tố xoài", "STX001", 2, 28000, 70, "Sinh tố xoài"),
                ("Trà đá", "TD001", 2, 8000, 300, "Trà đá vỉa hè"),
                ("Nước chanh", "NCH001", 2, 12000, 200, "Nước chanh tươi"),
                ("Sữa tươi", "ST001", 2, 10000, 250, "Sữa tươi Vinamilk"),
                ("Nước tăng lực", "NTL001", 2, 15000, 180, "Red Bull 250ml"),
                ("Bia Heineken", "BH001", 2, 25000, 120, "Bia Heineken 330ml"),
                ("Bia Tiger", "BT001", 2, 20000, 150, "Bia Tiger 330ml"),
                ("Rượu vang đỏ", "RVD001", 2, 150000, 30, "Rượu vang đỏ Chile"),
                ("Whisky", "W001", 2, 500000, 20, "Whisky Johnnie Walker"),
                ("Vodka", "V001", 2, 300000, 25, "Vodka Absolut"),
                ("Nước ép táo", "NET001", 2, 18000, 100, "Nước ép táo tươi"),
                ("Trà sữa matcha", "TSM001", 2, 28000, 60, "Trà sữa matcha Nhật"),

                // ĐIỆN TỬ (Category 3)
                ("iPhone 15", "IP001", 3, 25000000, 10, "Điện thoại iPhone 15 128GB"),
                ("Samsung Galaxy S24", "SG001", 3, 20000000, 15, "Điện thoại Samsung Galaxy S24"),
                ("Laptop Dell", "LD001", 3, 15000000, 5, "Laptop Dell Inspiron 15"),
                ("MacBook Air M2", "MBA001", 3, 28000000, 8, "MacBook Air M2 256GB"),
                ("iPad Pro", "IP001", 3, 18000000, 12, "iPad Pro 11 inch"),
                ("Samsung Galaxy Tab", "SGT001", 3, 8000000, 15, "Samsung Galaxy Tab S9"),
                ("AirPods Pro", "APP001", 3, 5000000, 20, "AirPods Pro 2nd gen"),
                ("Sony WH-1000XM5", "SW001", 3, 6000000, 10, "Tai nghe Sony WH-1000XM5"),
                ("Apple Watch Series 9", "AWS001", 3, 8000000, 15, "Apple Watch Series 9 GPS"),
                ("Samsung Galaxy Watch", "SGW001", 3, 5000000, 12, "Samsung Galaxy Watch 6"),
                ("PlayStation 5", "PS001", 3, 12000000, 8, "PlayStation 5 Digital"),
                ("Xbox Series X", "XSX001", 3, 11000000, 6, "Xbox Series X 1TB"),
                ("Nintendo Switch", "NS001", 3, 7000000, 10, "Nintendo Switch OLED"),
                ("Màn hình Dell 27 inch", "MD001", 3, 5000000, 20, "Màn hình Dell 27 inch 4K"),
                ("Bàn phím cơ", "BPC001", 3, 1500000, 25, "Bàn phím cơ Cherry MX"),
                ("Chuột gaming", "CG001", 3, 800000, 30, "Chuột gaming Logitech G Pro"),
                ("Webcam 4K", "W4K001", 3, 2000000, 15, "Webcam Logitech 4K Pro"),
                ("Microphone", "M001", 3, 1200000, 20, "Microphone Blue Yeti"),
                ("Router WiFi 6", "RW001", 3, 1800000, 18, "Router WiFi 6 ASUS"),
                ("Ổ cứng SSD 1TB", "SSD001", 3, 2000000, 40, "SSD Samsung 980 Pro 1TB"),

                // QUẦN ÁO (Category 4)
                ("Áo thun nam", "AT001", 4, 150000, 50, "Áo thun cotton nam size M"),
                ("Quần jean nữ", "QJ001", 4, 300000, 30, "Quần jean nữ size 28"),
                ("Giày thể thao", "GT001", 4, 500000, 20, "Giày thể thao Nike size 42"),
                ("Áo sơ mi nam", "ASM001", 4, 250000, 40, "Áo sơ mi nam trắng size L"),
                ("Váy liền nữ", "VL001", 4, 400000, 25, "Váy liền nữ đen size S"),
                ("Quần tây nam", "QT001", 4, 350000, 30, "Quần tây nam xám size 32"),
                ("Áo khoác nữ", "AK001", 4, 600000, 20, "Áo khoác nữ len size M"),
                ("Giày cao gót", "GCG001", 4, 450000, 15, "Giày cao gót nữ đen size 37"),
                ("Túi xách nữ", "TX001", 4, 800000, 12, "Túi xách nữ da thật"),
                ("Thắt lưng nam", "TL001", 4, 200000, 35, "Thắt lưng nam da bò"),
                ("Áo len nữ", "AL001", 4, 300000, 25, "Áo len nữ màu hồng size S"),
                ("Quần short nam", "QS001", 4, 180000, 40, "Quần short nam thể thao"),
                ("Áo dài nữ", "AD001", 4, 1200000, 8, "Áo dài nữ truyền thống"),
                ("Vest nam", "V001", 4, 1500000, 10, "Vest nam công sở"),
                ("Giày lười nam", "GL001", 4, 350000, 20, "Giày lười nam da thật"),
                ("Áo phông nữ", "AP001", 4, 120000, 60, "Áo phông nữ cotton size M"),
                ("Quần legging nữ", "QL001", 4, 150000, 45, "Quần legging nữ thể thao"),
                ("Mũ lưỡi trai", "MLT001", 4, 100000, 50, "Mũ lưỡi trai nam"),
                ("Khăn quàng cổ", "KQC001", 4, 80000, 30, "Khăn quàng cổ len"),
                ("Tất nam", "T001", 4, 50000, 100, "Tất nam cotton 5 đôi"),

                // GIA DỤNG (Category 5)
                ("Nồi cơm điện", "NC001", 5, 800000, 25, "Nồi cơm điện 1.8L"),
                ("Máy xay sinh tố", "MX001", 5, 600000, 15, "Máy xay sinh tố 2L"),
                ("Bàn ủi", "BU001", 5, 200000, 40, "Bàn ủi hơi nước"),
                ("Máy lọc nước", "MLN001", 5, 2500000, 8, "Máy lọc nước RO 9 cấp"),
                ("Tủ lạnh", "TL001", 5, 8000000, 5, "Tủ lạnh Samsung 300L"),
                ("Máy giặt", "MG001", 5, 6000000, 6, "Máy giặt LG 8kg"),
                ("Điều hòa", "DH001", 5, 10000000, 4, "Điều hòa Daikin 1.5HP"),
                ("Quạt điện", "QE001", 5, 500000, 30, "Quạt điện cây 3 cánh"),
                ("Bếp gas", "BG001", 5, 1200000, 12, "Bếp gas 2 bếp"),
                ("Lò vi sóng", "LVS001", 5, 2000000, 10, "Lò vi sóng Samsung 25L"),
                ("Máy hút bụi", "MHB001", 5, 1500000, 15, "Máy hút bụi Electrolux"),
                ("Bình nước nóng", "BNN001", 5, 1800000, 8, "Bình nước nóng 20L"),
                ("Máy sấy tóc", "MST001", 5, 300000, 25, "Máy sấy tóc Panasonic"),
                ("Bàn chải đánh răng điện", "BCDR001", 5, 800000, 20, "Bàn chải đánh răng điện Oral-B"),
                ("Máy ép trái cây", "MET001", 5, 700000, 12, "Máy ép trái cây tốc độ chậm"),
                ("Nồi áp suất", "NAS001", 5, 900000, 10, "Nồi áp suất điện 5L"),
                ("Máy pha cà phê", "MPC001", 5, 3000000, 5, "Máy pha cà phê tự động"),
                ("Bếp từ", "BT001", 5, 2000000, 8, "Bếp từ 2 vùng nấu"),
                ("Máy làm kem", "MLK001", 5, 400000, 15, "Máy làm kem tươi"),
                ("Bình giữ nhiệt", "BGN001", 5, 150000, 40, "Bình giữ nhiệt 500ml"),

                // SÁCH VỞ (Category 6)
                ("Sách lập trình", "SL001", 6, 150000, 20, "Sách học lập trình C#"),
                ("Vở học sinh", "VH001", 6, 10000, 100, "Vở học sinh 200 trang"),
                ("Bút bi", "BB001", 6, 5000, 200, "Bút bi xanh"),
                ("Sách tiếng Anh", "STA001", 6, 120000, 25, "Sách học tiếng Anh cơ bản"),
                ("Từ điển Anh-Việt", "TD001", 6, 200000, 15, "Từ điển Anh-Việt Oxford"),
                ("Bút chì", "BC001", 6, 3000, 300, "Bút chì 2B"),
                ("Tẩy", "T001", 6, 2000, 250, "Tẩy trắng"),
                ("Thước kẻ", "TK001", 6, 8000, 150, "Thước kẻ 30cm"),
                ("Compa", "C001", 6, 15000, 80, "Compa vẽ hình tròn"),
                ("Máy tính bỏ túi", "MTBT001", 6, 80000, 50, "Máy tính bỏ túi Casio"),
                ("Sách văn học", "SVH001", 6, 80000, 30, "Tuyển tập thơ Việt Nam"),
                ("Sách lịch sử", "SLS001", 6, 100000, 20, "Lịch sử Việt Nam"),
                ("Sách khoa học", "SKH001", 6, 180000, 15, "Khoa học vũ trụ"),
                ("Truyện tranh", "TT001", 6, 25000, 100, "Truyện tranh Doraemon"),
                ("Sách nấu ăn", "SNA001", 6, 120000, 25, "Sách dạy nấu ăn Việt Nam"),
                ("Bút highlight", "BH001", 6, 12000, 80, "Bút highlight màu vàng"),
                ("Giấy A4", "GA4001", 6, 50000, 200, "Giấy A4 500 tờ"),
                ("Bìa hồ sơ", "BHS001", 6, 15000, 100, "Bìa hồ sơ nhựa trong"),
                ("Kéo", "K001", 6, 25000, 60, "Kéo văn phòng"),
                ("Keo dán", "KD001", 6, 10000, 120, "Keo dán UHU"),

                // THỂ THAO (Category 7)
                ("Bóng đá", "BD001", 7, 200000, 30, "Bóng đá size 5"),
                ("Vợt cầu lông", "VC001", 7, 300000, 15, "Vợt cầu lông Yonex"),
                ("Giày chạy bộ", "GCB001", 7, 800000, 20, "Giày chạy bộ Nike Air Max"),
                ("Quần áo thể thao", "QATT001", 7, 250000, 40, "Bộ quần áo thể thao nam"),
                ("Găng tay boxing", "GTB001", 7, 150000, 25, "Găng tay boxing Everlast"),
                ("Bóng rổ", "BR001", 7, 180000, 20, "Bóng rổ Spalding"),
                ("Vợt tennis", "VT001", 7, 500000, 12, "Vợt tennis Wilson"),
                ("Xe đạp", "XD001", 7, 3000000, 8, "Xe đạp thể thao 21 tốc độ"),
                ("Dây nhảy", "DN001", 7, 50000, 50, "Dây nhảy thể thao"),
                ("Tạ tay", "TT001", 7, 200000, 30, "Tạ tay 5kg"),
                ("Thảm yoga", "TY001", 7, 120000, 25, "Thảm yoga cao cấp"),
                ("Bóng chuyền", "BC001", 7, 150000, 15, "Bóng chuyền Mikasa"),
                ("Kính bơi", "KB001", 7, 80000, 40, "Kính bơi Speedo"),
                ("Mũ bơi", "MB001", 7, 30000, 60, "Mũ bơi silicon"),
                ("Băng quấn tay", "BQT001", 7, 25000, 100, "Băng quấn tay boxing"),
                ("Bóng bàn", "BB001", 7, 100000, 20, "Bóng bàn Butterfly"),
                ("Vợt bóng bàn", "VBB001", 7, 200000, 15, "Vợt bóng bàn Stiga"),
                ("Giày bóng đá", "GBD001", 7, 600000, 18, "Giày bóng đá Adidas"),
                ("Áo bóng đá", "ABD001", 7, 300000, 25, "Áo bóng đá Real Madrid"),
                ("Băng đô thể thao", "BDT001", 7, 40000, 80, "Băng đô thể thao Nike"),

                // MỸ PHẨM (Category 8)
                ("Kem dưỡng da", "KD001", 8, 250000, 25, "Kem dưỡng da ban đêm"),
                ("Son môi", "SM001", 8, 120000, 40, "Son môi màu đỏ"),
                ("Kem chống nắng", "KCN001", 8, 180000, 30, "Kem chống nắng SPF 50+"),
                ("Sữa rửa mặt", "SRM001", 8, 150000, 35, "Sữa rửa mặt cho da dầu"),
                ("Toner", "T001", 8, 200000, 25, "Toner cân bằng độ pH"),
                ("Serum vitamin C", "SVC001", 8, 350000, 20, "Serum vitamin C chống lão hóa"),
                ("Mặt nạ", "MN001", 8, 80000, 50, "Mặt nạ dưỡng ẩm"),
                ("Kem mắt", "KM001", 8, 300000, 15, "Kem mắt chống quầng thâm"),
                ("Phấn nền", "PN001", 8, 220000, 20, "Phấn nền che khuyết điểm"),
                ("Mascara", "M001", 8, 160000, 25, "Mascara làm dài mi"),
                ("Eyeliner", "E001", 8, 100000, 30, "Eyeliner nước đen"),
                ("Phấn má hồng", "PMH001", 8, 140000, 20, "Phấn má hồng tự nhiên"),
                ("Kem che khuyết điểm", "KCK001", 8, 180000, 25, "Kem che khuyết điểm cao cấp"),
                ("Dầu dưỡng tóc", "DDT001", 8, 120000, 30, "Dầu dưỡng tóc Argan"),
                ("Dầu gội", "DG001", 8, 80000, 40, "Dầu gội cho tóc khô"),
                ("Sữa tắm", "ST001", 8, 60000, 50, "Sữa tắm dưỡng ẩm"),
                ("Kem dưỡng tay", "KDT001", 8, 70000, 35, "Kem dưỡng tay chống lão hóa"),
                ("Nước hoa", "NH001", 8, 800000, 10, "Nước hoa Chanel No.5"),
                ("Kem tẩy trang", "KTT001", 8, 90000, 30, "Kem tẩy trang dịu nhẹ"),
                ("Xịt khoáng", "XK001", 8, 110000, 25, "Xịt khoáng làm mát da"),

                // ĐỒ CHƠI (Category 9)
                ("Xe đồ chơi", "XD001", 9, 150000, 20, "Xe đồ chơi điều khiển"),
                ("Búp bê", "BB001", 9, 100000, 15, "Búp bê Barbie"),
                ("Lego", "L001", 9, 500000, 12, "Bộ lắp ráp Lego City"),
                ("Xe lửa đồ chơi", "XLD001", 9, 300000, 8, "Xe lửa đồ chơi chạy pin"),
                ("Bóng bay", "BB001", 9, 20000, 100, "Bóng bay nhiều màu"),
                ("Đồ chơi xếp hình", "DCXH001", 9, 80000, 25, "Đồ chơi xếp hình 100 mảnh"),
                ("Búp bê baby", "BBB001", 9, 200000, 10, "Búp bê baby biết khóc"),
                ("Xe đạp trẻ em", "XDTE001", 9, 800000, 6, "Xe đạp trẻ em 3 bánh"),
                ("Đồ chơi nấu ăn", "DCNA001", 9, 120000, 15, "Bộ đồ chơi nấu ăn"),
                ("Búp bê thay đồ", "BBTD001", 9, 150000, 12, "Búp bê thay đồ nhiều bộ"),
                ("Xe tải đồ chơi", "XTD001", 9, 100000, 18, "Xe tải đồ chơi lớn"),
                ("Đồ chơi bác sĩ", "DCBS001", 9, 180000, 10, "Bộ đồ chơi bác sĩ"),
                ("Bóng ném", "BN001", 9, 50000, 30, "Bóng ném mềm"),
                ("Đồ chơi âm nhạc", "DCAM001", 9, 200000, 8, "Đàn piano đồ chơi"),
                ("Xe máy đồ chơi", "XMD001", 9, 250000, 10, "Xe máy đồ chơi chạy pin"),
                ("Búp bê siêu nhân", "BBSN001", 9, 120000, 15, "Búp bê siêu nhân biến hình"),
                ("Đồ chơi xây dựng", "DCXD001", 9, 300000, 8, "Bộ đồ chơi xây dựng"),
                ("Bóng đá mini", "BDM001", 9, 80000, 20, "Bóng đá mini cho trẻ em"),
                ("Đồ chơi vẽ", "DCV001", 9, 60000, 25, "Bộ đồ chơi vẽ tranh"),
                ("Búp bê công chúa", "BBCP001", 9, 180000, 10, "Búp bê công chúa Disney"),

                // KHÁC (Category 10)
                ("Sản phẩm khác", "SP001", 10, 50000, 10, "Sản phẩm khác"),
                ("Voucher giảm giá", "VG001", 10, 100000, 50, "Voucher giảm giá 20%"),
                ("Thẻ quà tặng", "TQT001", 10, 200000, 30, "Thẻ quà tặng 200k"),
                ("Bao bì đóng gói", "BBDG001", 10, 15000, 200, "Bao bì đóng gói quà"),
                ("Dây buộc", "DB001", 10, 5000, 500, "Dây buộc đa năng"),
                ("Túi nilon", "TN001", 10, 2000, 1000, "Túi nilon sinh thái"),
                ("Băng keo", "BK001", 10, 10000, 300, "Băng keo trong"),
                ("Kẹp giấy", "KG001", 10, 8000, 400, "Kẹp giấy văn phòng"),
                ("Ghim bấm", "GB001", 10, 12000, 250, "Ghim bấm 24/6"),
                ("Bút lông", "BL001", 10, 15000, 150, "Bút lông dạ quang")
            };

            foreach (var (name, code, categoryId, price, stock, description) in defaultProducts)
            {
                try
                {
                    string insertCmd = "INSERT IGNORE INTO Products (Name, Code, CategoryId, salePrice, StockQuantity, Description) VALUES (@name, @code, @categoryId, @saleprice, @stock, @description);";
                    using var cmd = new MySqlCommand(insertCmd, connection);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@saleprice", price);
                    cmd.Parameters.AddWithValue("@stock", stock);
                    cmd.Parameters.AddWithValue("@description", description);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    // Product already exists, ignore
                }
            }
        }

        /// <summary>
        /// Xóa tất cả dữ liệu mẫu (chỉ dùng khi cần reset)
        /// </summary>
        public static void ClearSampleData()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            try
            {
                // Xóa dữ liệu theo thứ tự để tránh lỗi foreign key
                string[] deleteCommands = {
                    "DELETE FROM InvoiceItems;",
                    "DELETE FROM Invoices;", 
                    "DELETE FROM Products;",
                    "DELETE FROM Customers;",
                    "DELETE FROM Categories;"
                };

                foreach (var cmd in deleteCommands)
                {
                    using var deleteCmd = new MySqlCommand(cmd, connection);
                    deleteCmd.ExecuteNonQuery();
                }

                // Reset AUTO_INCREMENT cho tất cả bảng
                string[] resetCommands = {
                    "ALTER TABLE Categories AUTO_INCREMENT = 1;",
                    "ALTER TABLE Customers AUTO_INCREMENT = 1;",
                    "ALTER TABLE Products AUTO_INCREMENT = 1;",
                    "ALTER TABLE Invoices AUTO_INCREMENT = 1;",
                    "ALTER TABLE InvoiceItems AUTO_INCREMENT = 1;"
                };

                foreach (var cmd in resetCommands)
                {
                    try
                    {
                        using var resetCmd = new MySqlCommand(cmd, connection);
                        resetCmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resetting AUTO_INCREMENT: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa dữ liệu mẫu: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái database và dữ liệu mẫu
        /// </summary>
        public static string GetDatabaseStatus()
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                var status = new System.Text.StringBuilder();
                status.AppendLine("=== TRẠNG THÁI DATABASE ===");
                status.AppendLine($"Kết nối: ✅ Thành công");
                status.AppendLine($"Server: {connection.ServerVersion}");

                // Kiểm tra số lượng bản ghi
                string[] tables = { "Categories", "Products", "Customers", "Invoices", "InvoiceItems" };
                foreach (var table in tables)
                {
                    try
                    {
                        string countCmd = $"SELECT COUNT(*) FROM {table};";
                        using var cmd = new MySqlCommand(countCmd, connection);
                        long count = (long)cmd.ExecuteScalar();
                        status.AppendLine($"{table}: {count} bản ghi");
                    }
                    catch (Exception ex)
                    {
                        status.AppendLine($"{table}: ❌ Lỗi - {ex.Message}");
                    }
                }

                // Kiểm tra một vài sản phẩm mẫu
                status.AppendLine("\n=== SẢN PHẨM MẪU ===");
                string sampleCmd = "SELECT Name, salePrice, StockQuantity FROM Products LIMIT 5;";
                using var sampleCmdObj = new MySqlCommand(sampleCmd, connection);
                using var reader = sampleCmdObj.ExecuteReader();
                int i = 1;
                while (reader.Read() && i <= 5)
                {
                    status.AppendLine($"{i}. {reader.GetString(0)} - {reader.GetDecimal(1):N0}₫ - Tồn: {reader.GetInt32(2)}");
                    i++;
                }
                reader.Close();

                return status.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi kết nối database: {ex.Message}";
            }
        }

        /// <summary>
        /// Buộc tạo lại dữ liệu mẫu (bỏ qua kiểm tra)
        /// </summary>
        public static void ForceInitializeSampleData()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Xóa dữ liệu cũ trước
            ClearSampleData();

            // Tạo lại dữ liệu mẫu
            InitializeDefaultVietnameseData();
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

        /// <summary>
        /// Cập nhật cấu trúc cơ sở dữ liệu để phù hợp với ERD mới
        /// </summary>
        public static string MigrateToNewERD()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== BẮT ĐẦU MIGRATION DATABASE THEO ERD MỚI ===");
            
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                result.AppendLine("✅ Kết nối database thành công");

                // 1. Cập nhật bảng Accounts - thêm EmployeeName
                try
                {
                    string checkEmployeeNameCmd = "SHOW COLUMNS FROM Accounts LIKE 'EmployeeName';";
                    using var checkEmployeeName = new MySqlCommand(checkEmployeeNameCmd, connection);
                    var employeeNameExists = checkEmployeeName.ExecuteScalar();
                    
                    if (employeeNameExists == null)
                    {
                        string addEmployeeNameCmd = "ALTER TABLE Accounts ADD COLUMN EmployeeName VARCHAR(255) NOT NULL DEFAULT 'Employee';";
                        using var addEmployeeName = new MySqlCommand(addEmployeeNameCmd, connection);
                        addEmployeeName.ExecuteNonQuery();
                        
                        // Cập nhật username thành EmployeeName cho tài khoản admin
                        string updateAdminCmd = "UPDATE Accounts SET EmployeeName = CASE WHEN Username = 'admin' THEN 'Administrator' ELSE CONCAT('User ', Username) END;";
                        using var updateAdmin = new MySqlCommand(updateAdminCmd, connection);
                        updateAdmin.ExecuteNonQuery();
                        
                        result.AppendLine("✅ Đã thêm trường EmployeeName vào bảng Accounts");
                    }
                    else
                    {
                        result.AppendLine("⚠️ Trường EmployeeName đã tồn tại trong bảng Accounts");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"❌ Lỗi cập nhật bảng Accounts: {ex.Message}");
                }

                // 2. Cập nhật bảng Products - thêm PurchaseUnit, PurchasePrice và đổi Price thành SalePrice
                try
                {
                    // Kiểm tra PurchaseUnit
                    string checkPurchaseUnitCmd = "SHOW COLUMNS FROM Products LIKE 'PurchaseUnit';";
                    using var checkPurchaseUnit = new MySqlCommand(checkPurchaseUnitCmd, connection);
                    var purchaseUnitExists = checkPurchaseUnit.ExecuteScalar();
                    
                    if (purchaseUnitExists == null)
                    {
                        string addPurchaseUnitCmd = "ALTER TABLE Products ADD COLUMN PurchaseUnit VARCHAR(50) DEFAULT 'Piece';";
                        using var addPurchaseUnit = new MySqlCommand(addPurchaseUnitCmd, connection);
                        addPurchaseUnit.ExecuteNonQuery();
                        result.AppendLine("✅ Đã thêm trường PurchaseUnit vào bảng Products");
                    }

                    // Kiểm tra PurchasePrice
                    string checkPurchasePriceCmd = "SHOW COLUMNS FROM Products LIKE 'PurchasePrice';";
                    using var checkPurchasePrice = new MySqlCommand(checkPurchasePriceCmd, connection);
                    var purchasePriceExists = checkPurchasePrice.ExecuteScalar();
                    
                    if (purchasePriceExists == null)
                    {
                        string addPurchasePriceCmd = "ALTER TABLE Products ADD COLUMN PurchasePrice DECIMAL(10,2) DEFAULT 0.00;";
                        using var addPurchasePrice = new MySqlCommand(addPurchasePriceCmd, connection);
                        addPurchasePrice.ExecuteNonQuery();
                        
                        // Cập nhật PurchasePrice = SalePrice * 0.8 cho các sản phẩm hiện có
                        string updatePurchasePriceCmd = "UPDATE Products SET PurchasePrice = ROUND(Price * 0.8, 2) WHERE PurchasePrice = 0;";
                        using var updatePurchasePrice = new MySqlCommand(updatePurchasePriceCmd, connection);
                        updatePurchasePrice.ExecuteNonQuery();
                        
                        result.AppendLine("✅ Đã thêm trường PurchasePrice vào bảng Products");
                    }

                    // Kiểm tra SalePrice (đổi từ Price)
                    string checkSalePriceCmd = "SHOW COLUMNS FROM Products LIKE 'SalePrice';";
                    using var checkSalePrice = new MySqlCommand(checkSalePriceCmd, connection);
                    var salePriceExists = checkSalePrice.ExecuteScalar();
                    
                    if (salePriceExists == null && purchasePriceExists == null) // Chỉ làm khi chưa có cả hai
                    {
                        string addSalePriceCmd = "ALTER TABLE Products ADD COLUMN SalePrice DECIMAL(10,2) NOT NULL DEFAULT 0.00;";
                        using var addSalePrice = new MySqlCommand(addSalePriceCmd, connection);
                        addSalePrice.ExecuteNonQuery();
                        
                        // Copy giá trị từ Price sang SalePrice
                        string copyPriceCmd = "UPDATE Products SET SalePrice = Price;";
                        using var copyPrice = new MySqlCommand(copyPriceCmd, connection);
                        copyPrice.ExecuteNonQuery();
                        
                        // Xóa cột Price cũ
                        string dropPriceCmd = "ALTER TABLE Products DROP COLUMN Price;";
                        using var dropPrice = new MySqlCommand(dropPriceCmd, connection);
                        dropPrice.ExecuteNonQuery();
                        
                        result.AppendLine("✅ Đã đổi trường Price thành SalePrice trong bảng Products");
                    }
                    else if (salePriceExists == null && purchasePriceExists != null)
                    {
                        result.AppendLine("⚠️ Đã có PurchasePrice nhưng chưa có SalePrice, giữ nguyên Price");
                    }
                    else
                    {
                        result.AppendLine("⚠️ Trường SalePrice đã tồn tại trong bảng Products");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"❌ Lỗi cập nhật bảng Products: {ex.Message}");
                }

                // 3. Cập nhật bảng Customers - thêm LoyaltyPoints
                try
                {
                    string checkLoyaltyPointsCmd = "SHOW COLUMNS FROM Customers LIKE 'LoyaltyPoints';";
                    using var checkLoyaltyPoints = new MySqlCommand(checkLoyaltyPointsCmd, connection);
                    var loyaltyPointsExists = checkLoyaltyPoints.ExecuteScalar();
                    
                    if (loyaltyPointsExists == null)
                    {
                        string addLoyaltyPointsCmd = "ALTER TABLE Customers ADD COLUMN LoyaltyPoints INT DEFAULT 0;";
                    using var addLoyaltyPoints = new MySqlCommand(addLoyaltyPointsCmd, connection);
                        addLoyaltyPoints.ExecuteNonQuery();
                        result.AppendLine("✅ Đã thêm trường LoyaltyPoints vào bảng Customers");
                    }
                    else
                    {
                        result.AppendLine("⚠️ Trường LoyaltyPoints đã tồn tại trong bảng Customers");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"❌ Lỗi cập nhật bảng Customers: {ex.Message}");
                }

                // 4. Cập nhật bảng Invoices - thêm EmployeeId
                try
                {
                    string checkEmployeeIdCmd = "SHOW COLUMNS FROM Invoices LIKE 'EmployeeId';";
                    using var checkEmployeeId = new MySqlCommand(checkEmployeeIdCmd, connection);
                    var employeeIdExists = checkEmployeeId.ExecuteScalar();
                    
                    if (employeeIdExists == null)
                    {
                        // Thêm cột EmployeeId một cách cẩn thận
                        string addEmployeeIdCmd = "ALTER TABLE Invoices ADD COLUMN EmployeeId INT;";
                        using var addEmployeeId = new MySqlCommand(addEmployeeIdCmd, connection);
                        addEmployeeId.ExecuteNonQuery();
                        
                        // Lấy ID của admin để set làm EmployeeId mặc định cho các invoice hiện có
                        string getAdminIdCmd = "SELECT Id FROM Accounts WHERE Username = 'admin' LIMIT 1;";
                        using var getAdminId = new MySqlCommand(getAdminIdCmd, connection);
                        var adminIdObj = getAdminId.ExecuteScalar();
                        
                        if (adminIdObj != null)
                        {
                            int adminId = Convert.ToInt32(adminIdObj);
                            string updateInvoiceCmd = "UPDATE Invoices SET EmployeeId = @adminId WHERE EmployeeId IS NULL;";
                            using var updateInvoice = new MySqlCommand(updateInvoiceCmd, connection);
                            updateInvoice.Parameters.AddWithValue("@adminId", adminId);
                            updateInvoice.ExecuteNonQuery();
                        }
                        
                        // Thêm NOT NULL constraint sau khi cập nhật data
                        string alterEmployeeIdCmd = "ALTER TABLE Invoices MODIFY COLUMN EmployeeId INT NOT NULL;";
                        using var alterEmployeeId = new MySqlCommand(alterEmployeeIdCmd, connection);
                        alterEmployeeId.ExecuteNonQuery();
                        
                        // Thêm foreign key constraint
                        string addFKCommand = "ALTER TABLE Invoices ADD FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id);";
                        using var addFK = new MySqlCommand(addFKCommand, connection);
                        addFK.ExecuteNonQuery();
                        
                        result.AppendLine("✅ Đã thêm trường EmployeeId vào bảng Invoices");
                    }
                    else
                    {
                        result.AppendLine("⚠️ Trường EmployeeId đã tồn tại trong bảng Invoices");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"❌ Lỗi cập nhật bảng Invoices: {ex.Message}");
                }

                // 5. Cập nhật bảng InvoiceItems - thêm EmployeeId
                try
                {
                    string checkInvoiceItemsEmployeeIdCmd = "SHOW COLUMNS FROM InvoiceItems LIKE 'EmployeeId';";
                    using var checkInvoiceItemsEmployeeId = new MySqlCommand(checkInvoiceItemsEmployeeIdCmd, connection);
                    var invoiceItemsEmployeeIdExists = checkInvoiceItemsEmployeeId.ExecuteScalar();
                    
                    if (invoiceItemsEmployeeIdExists == null)
                    {
                        // Thêm cột EmployeeId
                        string addInvoiceItemsEmployeeIdCmd = "ALTER TABLE InvoiceItems ADD COLUMN EmployeeId INT;";
                        using var addInvoiceItemsEmployeeId = new MySqlCommand(addInvoiceItemsEmployeeIdCmd, connection);
                        addInvoiceItemsEmployeeId.ExecuteNonQuery();
                        
                        // Copy EmployeeId từ Invoices sang InvoiceItems
                        string updateInvoiceItemsCmd = @"UPDATE InvoiceItems ii 
                                                        JOIN Invoices i ON ii.InvoiceId = i.Id 
                                                        SET ii.EmployeeId = i.EmployeeId 
                                                        WHERE ii.EmployeeId IS NULL;";
                        using var updateInvoiceItems = new MySqlCommand(updateInvoiceItemsCmd, connection);
                        updateInvoiceItems.ExecuteNonQuery();
                        
                        // Thêm NOT NULL constraint
                        string alterInvoiceItemsEmployeeIdCmd = "ALTER TABLE InvoiceItems MODIFY COLUMN EmployeeId INT NOT NULL;";
                        using var alterInvoiceItemsEmployeeId = new MySqlCommand(alterInvoiceItemsEmployeeIdCmd, connection);
                        alterInvoiceItemsEmployeeId.ExecuteNonQuery();
                        
                        // Thêm foreign key constraint
                        string addInvoiceItemsFKCommand = "ALTER TABLE InvoiceItems ADD FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id);";
                        using var addInvoiceItemsFK = new MySqlCommand(addInvoiceItemsFKCommand, connection);
                        addInvoiceItemsFK.ExecuteNonQuery();
                        
                        result.AppendLine("✅ Đã thêm trường EmployeeId vào bảng InvoiceItems");
                    }
                    else
                    {
                        result.AppendLine("⚠️ Trường EmployeeId đã tồn tại trong bảng InvoiceItems");
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"❌ Lỗi cập nhật bảng InvoiceItems: {ex.Message}");
                }

                result.AppendLine("\n=== HOÀN THÀNH MIGRATION DATABASE ===");
                result.AppendLine("✅ Database đã được cập nhật để phù hợp với ERD mới");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ Lỗi kết nối database: {ex.Message}");
                return result.ToString();
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái database so với ERD mới
        /// </summary>
        public static string CheckERDCompatibility()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== KIỂM TRA TÍNH TƯƠNG THÍCH VỚI ERD MỚI ===");
            
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                result.AppendLine("✅ Kết nối database thành công\n");

                // Kiểm tra từng bảng theo ERD
                result.AppendLine("📋 TRẠNG THÁI CÁC BẢNG:");
                
                // 1. Bảng Accounts
                result.AppendLine("\n🔍 BẢNG ACCOUNTS:");
                var accountColumns = new[] { "Id", "Username", "EmployeeName", "Password", "Role" };
                foreach (var column in accountColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM Accounts LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // 2. Bảng Categories
                result.AppendLine("\n🔍 BẢNG CATEGORIES:");
                var categoryColumns = new[] { "Id", "Name" };
                foreach (var column in categoryColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM Categories LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // 3. Bảng Products
                result.AppendLine("\n🔍 BẢNG PRODUCTS:");
                var productColumns = new[] { "Id", "Name", "CategoryId", "StockQuantity", "Code", "Description", "PurchaseUnit", "PurchasePrice", "SalePrice", "CreatedDate", "UpdatedDate" };
                foreach (var column in productColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM Products LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // 4. Bảng Customers
                result.AppendLine("\n🔍 BẢNG CUSTOMERS:");
                var customerColumns = new[] { "Id", "Name", "Phone", "Email", "Address", "CustomerType", "LoyaltyPoints", "CreatedDate", "UpdatedDate" };
                foreach (var column in customerColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM Customers LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // 5. Bảng Invoices
                result.AppendLine("\n🔍 BẢNG INVOICES:");
                var invoiceColumns = new[] { "Id", "CustomerId", "EmployeeId", "Subtotal", "TaxPercent", "TaxAmount", "Discount", "Total", "Paid", "CreatedDate" };
                foreach (var column in invoiceColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM Invoices LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // 6. Bảng InvoiceItems
                result.AppendLine("\n🔍 BẢNG INVOICEITEMS:");
                var invoiceItemColumns = new[] { "Id", "InvoiceId", "ProductId", "EmployeeId", "UnitPrice", "Quantity", "LineTotal" };
                foreach (var column in invoiceItemColumns)
                {
                    string checkCmd = $"SHOW COLUMNS FROM InvoiceItems LIKE '{column}';";
                    using var check = new MySqlCommand(checkCmd, connection);
                    var exists = check.ExecuteScalar() != null;
                    result.AppendLine($"  - {column}: {(exists ? "✅" : "❌")} {(exists ? "Có" : "Thiếu")}");
                }

                // Kiểm tra dữ liệu mẫu
                result.AppendLine("\n📊 DỮ LIỆU HIỆN TẠI:");
                string[] tableNames = { "Accounts", "Categories", "Customers", "Products", "Invoices", "InvoiceItems" };
               foreach (var tableName in tableNames)
                {
                    try
                    {
                        string countCmd = $"SELECT COUNT(*) FROM {tableName};";
                        using var countCmdObj = new MySqlCommand(countCmd, connection);
                        long count = (long)countCmdObj.ExecuteScalar();
                        result.AppendLine($"  - {tableName}: {count} bản ghi");
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"  - {tableName}: ❌ Lỗi - {ex.Message}");
                    }
                }

                result.AppendLine("\n=== KẾT LUẬN ===");
                result.AppendLine("📝 Một số trường có thể không thực sự được yêu cầu hoàn toàn cho chức năng hệ thống");
                result.AppendLine("💡 Chỉ cần chạy MigrateToNewERD() để cập nhật cấu trúc");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ Lỗi kiểm tra database: {ex.Message}");
                return result.ToString();
            }
        }

        /// <summary>
        /// Tạo dữ liệu mẫu ngay lập tức (không kiểm tra gì cả)
        /// </summary>
        public static string CreateSampleDataNow()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== BẮT ĐẦU TẠO DỮ LIỆU MẪU ===");
            
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                result.AppendLine("✅ Kết nối database thành công");

                // Add default Vietnamese categories
                var defaultCategories = new[]
                {
                    "Thực phẩm",
                    "Đồ uống", 
                    "Điện tử",
                    "Quần áo",
                    "Gia dụng",
                    "Sách vở",
                    "Thể thao",
                    "Mỹ phẩm",
                    "Đồ chơi",
                    "Khác"
                };

                int categoriesAdded = 0;
                foreach (var category in defaultCategories)
                {
                    try
                    {
                        string insertCmd = "INSERT IGNORE INTO Categories (Name) VALUES (@name);";
                        using var cmd = new MySqlCommand(insertCmd, connection);
                        cmd.Parameters.AddWithValue("@name", category);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0) categoriesAdded++;
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"❌ Lỗi thêm danh mục '{category}': {ex.Message}");
                    }
                }
                result.AppendLine($"✅ Đã thêm {categoriesAdded} danh mục");

                // Kiểm tra danh mục đã tạo
                string checkCategoriesCmd = "SELECT Id, Name FROM Categories ORDER BY Id;";
                using var checkCatCmd = new MySqlCommand(checkCategoriesCmd, connection);
                using var reader = checkCatCmd.ExecuteReader();
                result.AppendLine("\n📋 Danh mục hiện có:");
                while (reader.Read())
                {
                    result.AppendLine($"  - ID: {reader["Id"]}, Tên: {reader["Name"]}");
                }
                reader.Close();

                // Add default Vietnamese customers
                var defaultCustomers = new[]
                {
                    ("Khách lẻ", "", "", "Thường", ""),
                    ("Nguyễn Văn An", "0123456789", "an.nguyen@email.com", "VIP", "123 Đường ABC, Quận 1, TP.HCM"),
                    ("Trần Thị Bình", "0987654321", "binh.tran@email.com", "Thường", "456 Đường XYZ, Quận 2, TP.HCM"),
                    ("Lê Văn Cường", "0369258147", "cuong.le@email.com", "Sỉ", "789 Đường DEF, Quận 3, TP.HCM"),
                    ("Phạm Thị Dung", "0741852963", "dung.pham@email.com", "Doanh nghiệp", "321 Đường GHI, Quận 4, TP.HCM")
                };

                int customersAdded = 0;
                foreach (var (name, phone, email, type, address) in defaultCustomers)
                {
                    try
                    {
                        string insertCmd = "INSERT IGNORE INTO Customers (Name, Phone, Email, CustomerType, Address) VALUES (@name, @phone, @email, @type, @address);";
                        using var cmd = new MySqlCommand(insertCmd, connection);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.Parameters.AddWithValue("@address", address);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0) customersAdded++;
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"❌ Lỗi thêm khách hàng '{name}': {ex.Message}");
                    }
                }
                result.AppendLine($"✅ Đã thêm {customersAdded} khách hàng");

                // Add many sample products
                var sampleProducts = new[]
                {
                    // Thực phẩm (CategoryId = 1)
                    ("Cơm tấm sườn nướng", "CT001", 1, 35000, 100, "Cơm tấm với sườn nướng thơm ngon"),
                    ("Phở bò", "PB001", 1, 45000, 50, "Phở bò truyền thống"),
                    ("Bánh mì thịt nướng", "BM001", 1, 15000, 200, "Bánh mì với thịt nướng"),
                    ("Bún bò Huế", "BBH001", 1, 40000, 80, "Bún bò Huế đặc sản"),
                    ("Chả cá Lã Vọng", "CCL001", 1, 55000, 60, "Chả cá Lã Vọng Hà Nội"),
                    ("Gỏi cuốn tôm thịt", "GCT001", 1, 25000, 120, "Gỏi cuốn tôm thịt tươi ngon"),
                    ("Bánh xèo", "BX001", 1, 30000, 90, "Bánh xèo miền Tây"),
                    ("Nem nướng Nha Trang", "NN001", 1, 35000, 70, "Nem nướng Nha Trang đặc sản"),
                    ("Bánh canh cua", "BCC001", 1, 40000, 85, "Bánh canh cua miền Trung"),
                    ("Chè đậu đỏ", "CD001", 1, 15000, 150, "Chè đậu đỏ ngọt mát"),
                    
                    // Đồ uống (CategoryId = 2)
                    ("Coca Cola", "CO001", 2, 12000, 300, "Nước ngọt Coca Cola 330ml"),
                    ("Nước suối", "NS001", 2, 5000, 500, "Nước suối tinh khiết 500ml"),
                    ("Trà sữa trân châu", "TS001", 2, 25000, 200, "Trà sữa trân châu đen"),
                    ("Cà phê sữa đá", "CF001", 2, 15000, 180, "Cà phê sữa đá Việt Nam"),
                    ("Nước cam ép", "NC001", 2, 20000, 120, "Nước cam ép tươi 100%"),
                    ("Sinh tố bơ", "SB001", 2, 30000, 100, "Sinh tố bơ béo ngậy"),
                    ("Trà đào cam sả", "TD001", 2, 22000, 150, "Trà đào cam sả thơm ngon"),
                    ("Nước dừa tươi", "ND001", 2, 18000, 200, "Nước dừa tươi nguyên chất"),
                    ("Sữa chua dẻo", "SC001", 2, 12000, 250, "Sữa chua dẻo vị dâu"),
                    ("Nước chanh dây", "NCD001", 2, 16000, 180, "Nước chanh dây mát lạnh"),
                    
                    // Điện tử (CategoryId = 3)
                    ("iPhone 15", "IP001", 3, 25000000, 10, "Điện thoại iPhone 15 128GB"),
                    ("Samsung Galaxy S24", "SG001", 3, 20000000, 15, "Điện thoại Samsung Galaxy S24"),
                    ("MacBook Air M2", "MB001", 3, 35000000, 8, "Laptop MacBook Air M2 13 inch"),
                    ("iPad Pro 12.9", "IPD001", 3, 28000000, 12, "Máy tính bảng iPad Pro 12.9 inch"),
                    ("AirPods Pro", "AP001", 3, 6000000, 25, "Tai nghe AirPods Pro 2"),
                    ("Sony WH-1000XM5", "SW001", 3, 8000000, 20, "Tai nghe chống ồn Sony"),
                    ("Apple Watch Series 9", "AW001", 3, 12000000, 18, "Đồng hồ thông minh Apple Watch"),
                    ("Samsung 4K TV 55", "ST001", 3, 15000000, 5, "TV Samsung 4K 55 inch"),
                    ("PlayStation 5", "PS001", 3, 12000000, 8, "Máy chơi game PlayStation 5"),
                    ("Nintendo Switch", "NS001", 3, 8000000, 15, "Máy chơi game Nintendo Switch"),
                    
                    // Quần áo (CategoryId = 4)
                    ("Áo thun nam", "AT001", 4, 150000, 50, "Áo thun cotton nam size M"),
                    ("Quần jean nữ", "QJ001", 4, 300000, 30, "Quần jean nữ size 28"),
                    ("Giày thể thao", "GT001", 4, 500000, 20, "Giày thể thao Nike size 42"),
                    ("Áo sơ mi nam", "AS001", 4, 250000, 40, "Áo sơ mi nam công sở"),
                    ("Váy liền nữ", "VL001", 4, 400000, 25, "Váy liền nữ dự tiệc"),
                    ("Quần short nam", "QS001", 4, 180000, 60, "Quần short nam thể thao"),
                    ("Áo khoác nữ", "AK001", 4, 600000, 20, "Áo khoác nữ mùa đông"),
                    ("Giày cao gót", "GCH001", 4, 350000, 15, "Giày cao gót nữ size 37"),
                    ("Túi xách nữ", "TX001", 4, 800000, 12, "Túi xách nữ da thật"),
                    ("Thắt lưng nam", "TL001", 4, 200000, 35, "Thắt lưng nam da bò"),
                    
                    // Gia dụng (CategoryId = 5)
                    ("Nồi cơm điện", "NC001", 5, 800000, 25, "Nồi cơm điện 1.8L"),
                    ("Máy xay sinh tố", "MX001", 5, 600000, 30, "Máy xay sinh tố đa năng"),
                    ("Bình giữ nhiệt", "BG001", 5, 150000, 50, "Bình giữ nhiệt 500ml"),
                    ("Chảo chống dính", "CCD001", 5, 200000, 40, "Chảo chống dính 24cm"),
                    ("Máy lọc nước", "ML001", 5, 2500000, 8, "Máy lọc nước RO 8 cấp"),
                    ("Quạt điện", "QD001", 5, 400000, 35, "Quạt điện đứng 3 cánh"),
                    ("Đèn bàn LED", "DL001", 5, 180000, 45, "Đèn bàn LED điều chỉnh độ sáng"),
                    ("Bàn ủi hơi nước", "BU001", 5, 350000, 20, "Bàn ủi hơi nước 1800W"),
                    ("Máy sấy tóc", "MS001", 5, 250000, 30, "Máy sấy tóc 2000W"),
                    ("Bộ dao kéo", "BD001", 5, 300000, 25, "Bộ dao kéo inox 6 món"),
                    
                    // Sách vở (CategoryId = 6)
                    ("Sách lập trình C#", "SC001", 6, 150000, 20, "Sách học lập trình C# từ cơ bản"),
                    ("Vở học sinh", "VH001", 6, 8000, 200, "Vở học sinh 200 trang"),
                    ("Bút bi xanh", "BBX001", 6, 3000, 500, "Bút bi xanh 0.5mm"),
                    ("Sách tiếng Anh", "ST001", 6, 120000, 30, "Sách học tiếng Anh giao tiếp"),
                    ("Từ điển Anh-Việt", "TD001", 6, 200000, 15, "Từ điển Anh-Việt 50000 từ"),
                    ("Bút chì 2B", "BC2B001", 6, 2000, 300, "Bút chì 2B gỗ"),
                    ("Sách toán học", "SM001", 6, 100000, 25, "Sách toán học lớp 12"),
                    ("Bút highlight", "BH001", 6, 5000, 150, "Bút highlight màu vàng"),
                    ("Sách văn học", "SV001", 6, 80000, 40, "Tuyển tập thơ văn Việt Nam"),
                    ("Tẩy gôm", "TG001", 6, 1000, 400, "Tẩy gôm trắng"),
                    
                    // Thể thao (CategoryId = 7)
                    ("Bóng đá", "BD001", 7, 200000, 30, "Bóng đá size 5 chính thức"),
                    ("Vợt cầu lông", "VC001", 7, 300000, 20, "Vợt cầu lông Yonex"),
                    ("Quần áo thể thao", "QT001", 7, 180000, 50, "Bộ quần áo thể thao nam"),
                    ("Giày chạy bộ", "GCB001", 7, 800000, 15, "Giày chạy bộ Nike Air Max"),
                    ("Găng tay boxing", "GB001", 7, 150000, 25, "Găng tay boxing Everlast"),
                    ("Bóng rổ", "BR001", 7, 250000, 20, "Bóng rổ size 7"),
                    ("Thảm yoga", "TY001", 7, 120000, 40, "Thảm yoga cao cấp"),
                    ("Dây nhảy", "DN001", 7, 50000, 60, "Dây nhảy thể dục"),
                    ("Tạ tay", "TT001", 7, 200000, 35, "Tạ tay 5kg"),
                    ("Bóng tennis", "BT001", 7, 80000, 100, "Bóng tennis Wilson"),
                    
                    // Mỹ phẩm (CategoryId = 8)
                    ("Kem dưỡng da", "KD001", 8, 300000, 25, "Kem dưỡng da ban đêm"),
                    ("Son môi", "SM001", 8, 150000, 40, "Son môi màu đỏ"),
                    ("Sữa rửa mặt", "SR001", 8, 120000, 50, "Sữa rửa mặt cho da dầu"),
                    ("Kem chống nắng", "KC001", 8, 200000, 30, "Kem chống nắng SPF 50"),
                    ("Nước hoa", "NH001", 8, 800000, 15, "Nước hoa nữ Chanel"),
                    ("Mascara", "MA001", 8, 180000, 35, "Mascara làm dài mi"),
                    ("Kem nền", "KN001", 8, 250000, 20, "Kem nền che khuyết điểm"),
                    ("Toner", "TO001", 8, 100000, 45, "Toner cân bằng da"),
                    ("Serum vitamin C", "SV001", 8, 400000, 18, "Serum vitamin C sáng da"),
                    ("Mặt nạ", "MN001", 8, 80000, 60, "Mặt nạ dưỡng ẩm"),
                    
                    // Đồ chơi (CategoryId = 9)
                    ("Xe điều khiển", "XD001", 9, 500000, 15, "Xe điều khiển từ xa"),
                    ("Búp bê Barbie", "BBB001", 9, 300000, 20, "Búp bê Barbie thời trang"),
                    ("Lego xây dựng", "LX001", 9, 800000, 12, "Bộ Lego xây dựng thành phố"),
                    ("Robot biến hình", "RB001", 9, 600000, 10, "Robot biến hình Transformers"),
                    ("Bảng vẽ điện tử", "BV001", 9, 1200000, 8, "Bảng vẽ điện tử cho trẻ em"),
                    ("Đồ chơi nấu ăn", "DC001", 9, 200000, 25, "Bộ đồ chơi nấu ăn mini"),
                    ("Puzzle 1000 mảnh", "PU001", 9, 150000, 30, "Puzzle 1000 mảnh phong cảnh"),
                    ("Máy bay điều khiển", "MBD001", 9, 1000000, 5, "Máy bay điều khiển từ xa"),
                    ("Bộ cờ vua", "CV001", 9, 100000, 40, "Bộ cờ vua cao cấp"),
                    ("Đồ chơi xếp hình", "DX001", 9, 120000, 35, "Đồ chơi xếp hình 3D"),
                    
                    // Khác (CategoryId = 10)
                    ("Khăn tắm", "KT001", 10, 80000, 50, "Khăn tắm cotton 100%"),
                    ("Gối ngủ", "GN001", 10, 150000, 30, "Gối ngủ memory foam"),
                    ("Chăn mền", "CM001", 10, 300000, 20, "Chăn mền ấm mùa đông"),
                    ("Đồng hồ treo tường", "DT001", 10, 200000, 25, "Đồng hồ treo tường hiện đại"),
                    ("Lọ hoa trang trí", "LH001", 10, 120000, 40, "Lọ hoa trang trí phòng khách"),
                    ("Thảm trải sàn", "TT001", 10, 400000, 15, "Thảm trải sàn cao cấp"),
                    ("Đèn ngủ", "DN001", 10, 100000, 35, "Đèn ngủ LED cảm ứng"),
                    ("Bình hoa", "BH001", 10, 80000, 45, "Bình hoa gốm sứ"),
                    ("Gương trang điểm", "GT001", 10, 250000, 20, "Gương trang điểm có đèn LED"),
                    ("Kệ sách", "KS001", 10, 500000, 10, "Kệ sách gỗ 5 tầng")
                };

                int productsAdded = 0;
                foreach (var (name, baseCode, categoryId, price, stock, description) in sampleProducts)
                {
                    try
                    {
                        // Kiểm tra categoryId có tồn tại không
                        string checkCategoryCmd = "SELECT COUNT(*) FROM Categories WHERE Id = @categoryId;";
                        using var checkCat = new MySqlCommand(checkCategoryCmd, connection);
                        checkCat.Parameters.AddWithValue("@categoryId", categoryId);
                        int categoryExists = Convert.ToInt32(checkCat.ExecuteScalar());
                        
                        if (categoryExists == 0)
                        {
                            result.AppendLine($"⚠️ Danh mục ID {categoryId} không tồn tại cho sản phẩm '{name}'");
                            continue;
                        }

                        // Tạo mã sản phẩm duy nhất
                        string uniqueCode = GenerateUniqueProductCode(baseCode, connection);

                        string insertCmd = "INSERT INTO Products (Name, Code, CategoryId, salePrice, StockQuantity, Description) VALUES (@name, @code, @categoryId, @saleprice, @stock, @description);";
                        using var cmd = new MySqlCommand(insertCmd, connection);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@code", uniqueCode);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        cmd.Parameters.AddWithValue("@saleprice", price);
                        cmd.Parameters.AddWithValue("@stock", stock);
                        cmd.Parameters.AddWithValue("@description", description);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0) 
                        {
                            productsAdded++;
                            result.AppendLine($"✅ Thêm sản phẩm '{name}' (Mã: {uniqueCode}) thành công");
                        }
                        else
                        {
                            result.AppendLine($"⚠️ Sản phẩm '{name}' không thể thêm");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"❌ Lỗi thêm sản phẩm '{name}': {ex.Message}");
                    }
                }
                result.AppendLine($"✅ Đã thêm {productsAdded} sản phẩm");

                // Kiểm tra sản phẩm đã tạo
                string checkProductsCmd = "SELECT COUNT(*) FROM Products;";
                using var checkProdCmd = new MySqlCommand(checkProductsCmd, connection);
                int totalProducts = Convert.ToInt32(checkProdCmd.ExecuteScalar());
                result.AppendLine($"\n📦 Tổng số sản phẩm trong database: {totalProducts}");

                result.AppendLine("\n=== HOÀN THÀNH TẠO DỮ LIỆU MẪU ===");
                result.AppendLine($"📊 Tổng kết: {categoriesAdded} danh mục, {customersAdded} khách hàng, {productsAdded} sản phẩm");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ Lỗi kết nối database: {ex.Message}");
                return result.ToString();
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

        // Tạo dữ liệu cửa hàng thời trang
        public static string CreateFashionStoreData()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== TAO DU LIEU CUA HANG THOI TRANG ===");

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

                result.AppendLine("✓ Ket noi database thanh cong");

                // Xóa dữ liệu cũ
                try
                {
                    using var deleteProducts = new MySqlCommand("DELETE FROM Products;", connection);
                    deleteProducts.ExecuteNonQuery();
                    using var deleteCategories = new MySqlCommand("DELETE FROM Categories;", connection);
                    deleteCategories.ExecuteNonQuery();
                    using var deleteCustomers = new MySqlCommand("DELETE FROM Customers WHERE Id > 1;", connection);
                    deleteCustomers.ExecuteNonQuery();
                    result.AppendLine("✓ Da xoa du lieu cu");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"! Khong the xoa du lieu cu: {ex.Message}");
                }

                // Danh mục
                var fashionCategories = new[]
                {
                    "Ao Nam", "Quan Nam", "Ao Nu", "Quan/Vay Nu", "Giay Dep",
                    "Tui Xach", "Phu Kien", "Dong Ho", "Trang Suc", "Do Lot"
                };

                foreach (var category in fashionCategories)
                {
                    string insertCmd = "INSERT INTO Categories (Name) VALUES (@name);";
                    using var cmd = new MySqlCommand(insertCmd, connection);
                    cmd.Parameters.AddWithValue("@name", category);
                    cmd.ExecuteNonQuery();
                }
                result.AppendLine("✓ Da them 10 danh muc");

                // Sản phẩm mẫu (dùng SalePrice thay Price)
                string insertProductSql = @"
                    INSERT INTO Products (Name, Code, CategoryId, SalePrice, StockQuantity, Description)
                    VALUES (@name, @code, @categoryId, @salePrice, @stock, @description);";

                var products = new[]
                {
                    ("Ao Thun Nam Basic", "ATN001", 1, 150000, 50, "Ao thun nam cotton 100%"),
                    ("Ao Polo Nam", "APN001", 1, 250000, 30, "Ao polo nam cao cap"),
                    ("Quan Jeans Nam Slim", "QJN001", 2, 400000, 40, "Quan jeans nam dang slim fit"),
                    ("Quan Kaki Nam", "QKN001", 2, 280000, 45, "Quan kaki nam phong cach cong so"),
                    ("Ao Thun Nu Crop Top", "ATU001", 3, 120000, 60, "Ao thun nu dang crop top")
                };

                foreach (var (name, code, categoryId, salePrice, stock, description) in products)
                {
                    using var cmd = new MySqlCommand(insertProductSql, connection);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@salePrice", salePrice);
                    cmd.Parameters.AddWithValue("@stock", stock);
                    cmd.Parameters.AddWithValue("@description", description);
                    cmd.ExecuteNonQuery();
                }

                result.AppendLine("✓ Da them san pham mau");
                return result.ToString();
            }
            catch (Exception ex)
            {
                result.AppendLine($"! Loi ket noi database: {ex.Message}");
                return result.ToString();
            }
        }
    }
}