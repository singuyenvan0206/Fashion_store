using MySql.Data.MySqlClient;

namespace WpfApp1
{
    public static class DatabaseHelper
    {
        private static string ConnectionString => SettingsManager.BuildConnectionString();

        public static void InitializeDatabase()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            string tableCmd = @"CREATE TABLE IF NOT EXISTS Accounts (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Username VARCHAR(255) NOT NULL UNIQUE,
                Password VARCHAR(255) NOT NULL,
                Role VARCHAR(20) NOT NULL DEFAULT 'Cashier'
            );";
            using var cmd = new MySqlCommand(tableCmd, connection);
            cmd.ExecuteNonQuery();

            string categoryCmd = @"CREATE TABLE IF NOT EXISTS Categories (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255) NOT NULL UNIQUE
            );";
            using var catCmd = new MySqlCommand(categoryCmd, connection);
            catCmd.ExecuteNonQuery();

            string productCmd = @"CREATE TABLE IF NOT EXISTS Products (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255) NOT NULL,
                Code VARCHAR(50) UNIQUE,
                CategoryId INT,
                SalePrice DECIMAL(10,2) NOT NULL,
                PurchasePrice DECIMAL(10,2) DEFAULT 0,
                PurchaseUnit VARCHAR(50) DEFAULT 'Cái',
                ImportQuantity INT DEFAULT 0,
                StockQuantity INT NOT NULL DEFAULT 0,
                Description TEXT,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );";
            using var prodCmd = new MySqlCommand(productCmd, connection);
            prodCmd.ExecuteNonQuery();

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

            UpdateProductsTable(connection);
            FixExistingProductData(connection);

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

                // Check if PurchasePrice column exists
                string checkPurchasePriceCmd = "SHOW COLUMNS FROM Products LIKE 'PurchasePrice';";
                using var checkPurchasePrice = new MySqlCommand(checkPurchasePriceCmd, connection);
                var purchasePriceExists = checkPurchasePrice.ExecuteScalar();

                if (purchasePriceExists == null)
                {
                    string addPurchasePriceCmd = "ALTER TABLE Products ADD COLUMN PurchasePrice DECIMAL(10,2) DEFAULT 0 AFTER SalePrice;";
                    using var addPurchasePrice = new MySqlCommand(addPurchasePriceCmd, connection);
                    addPurchasePrice.ExecuteNonQuery();
                }

                // Check if PurchaseUnit column exists
                string checkPurchaseUnitCmd = "SHOW COLUMNS FROM Products LIKE 'PurchaseUnit';";
                using var checkPurchaseUnit = new MySqlCommand(checkPurchaseUnitCmd, connection);
                var purchaseUnitExists = checkPurchaseUnit.ExecuteScalar();

                if (purchaseUnitExists == null)
                {
                    string addPurchaseUnitCmd = "ALTER TABLE Products ADD COLUMN PurchaseUnit VARCHAR(50) DEFAULT 'Cái' AFTER PurchasePrice;";
                    using var addPurchaseUnit = new MySqlCommand(addPurchaseUnitCmd, connection);
                    addPurchaseUnit.ExecuteNonQuery();
                }

                // Check if ImportQuantity column exists
                string checkImportQtyCmd = "SHOW COLUMNS FROM Products LIKE 'ImportQuantity';";
                using var checkImportQty = new MySqlCommand(checkImportQtyCmd, connection);
                var importQtyExists = checkImportQty.ExecuteScalar();

                if (importQtyExists == null)
                {
                    string addImportQtyCmd = "ALTER TABLE Products ADD COLUMN ImportQuantity INT DEFAULT 0 AFTER PurchaseUnit;";
                    using var addImportQty = new MySqlCommand(addImportQtyCmd, connection);
                    addImportQty.ExecuteNonQuery();
                }

            }
            catch
            {
                // Silent failure
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
            catch
            {
                // Silent failure
            }
        }

        public static bool RegisterAccount(string username, string employeeName, string password, string role = "Cashier")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("INSERT INTO Accounts (Username, EmployeeName, Password, Role) VALUES (@username, @employeeName, @password, @role);", connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@employeeName", employeeName);
            cmd.Parameters.AddWithValue("@password", password);
            cmd.Parameters.AddWithValue("@role", role);
            try { return cmd.ExecuteNonQuery() > 0; }
            catch { return false; }
        }

        public static string ValidateLogin(string username, string password)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@password;", connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);
            return (long)cmd.ExecuteScalar() > 0 ? "true" : "false";
        }

        public static string GetUserRole(string username)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT Role FROM Accounts WHERE Username=@username;", connection);
            cmd.Parameters.AddWithValue("@username", username);
            return cmd.ExecuteScalar()?.ToString() ?? "Cashier";
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

            string verifyCmd = "SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@oldPassword;";
            using var verify = new MySqlCommand(verifyCmd, connection);
            verify.Parameters.AddWithValue("@username", username);
            verify.Parameters.AddWithValue("@oldPassword", oldPassword);
            long count = (long)verify.ExecuteScalar();

            if (count == 0)
                return false;

            string updateCmd = "UPDATE Accounts SET Password=@newPassword WHERE Username=@username;";
            using var update = new MySqlCommand(updateCmd, connection);
            update.Parameters.AddWithValue("@username", username);
            update.Parameters.AddWithValue("@newPassword", newPassword);
            return update.ExecuteNonQuery() > 0;
        }

        public static List<(int Id, string Username, string EmployeeName)> GetAllAccounts()
        {
            var accounts = new List<(int, string, string)>();
            using var connection2 = new MySqlConnection(ConnectionString);
            connection2.Open();
            string selectCmd = "SELECT Id, Username, COALESCE(EmployeeName, '') FROM Accounts;";
            using var cmd2 = new MySqlCommand(selectCmd, connection2);
            using var reader2 = cmd2.ExecuteReader();
            while (reader2.Read())
            {
                accounts.Add((reader2.GetInt32(0), reader2.GetString(1), reader2.IsDBNull(2) ? "" : reader2.GetString(2)));
            }
            return accounts;
        }

        public static string GetEmployeeName(string username)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                string selectCmd = "SELECT COALESCE(EmployeeName, Username) FROM Accounts WHERE Username = @username;";
                using var cmd = new MySqlCommand(selectCmd, connection);
                cmd.Parameters.AddWithValue("@username", username);
                var result = cmd.ExecuteScalar();
                
                return result?.ToString() ?? username; // Fallback to username if EmployeeName is null
            }
            catch
            {
                return username; // Fallback to username on error
            }
        }

        public static int GetEmployeeIdByUsername(string username)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                string selectCmd = "SELECT Id FROM Accounts WHERE Username = @username;";
                using var cmd = new MySqlCommand(selectCmd, connection);
                cmd.Parameters.AddWithValue("@username", username);
                var result = cmd.ExecuteScalar();
                
                return result != null ? Convert.ToInt32(result) : 1; // Default to admin ID
            }
            catch
            {
                return 1; // Default to admin ID on error
            }
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


        public static bool DeleteAllAccountsExceptAdmin()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

            try
            {
                string sql = "DELETE FROM Accounts WHERE LOWER(Username) <> 'admin';";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static bool AddProduct(string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int importQuantity, int stockQuantity, string description = "")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            string cmdText = "INSERT INTO Products (Name, Code, CategoryId, SalePrice, PurchasePrice, PurchaseUnit, ImportQuantity, StockQuantity, Description) VALUES (@name, @code, @categoryId, @salePrice, @purchasePrice, @purchaseUnit, @importQuantity, @stockQuantity, @description);";
            using var cmd = new MySqlCommand(cmdText, connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@salePrice", salePrice);
            cmd.Parameters.AddWithValue("@purchasePrice", purchasePrice);
            cmd.Parameters.AddWithValue("@purchaseUnit", purchaseUnit);
            cmd.Parameters.AddWithValue("@importQuantity", importQuantity);
            cmd.Parameters.AddWithValue("@stockQuantity", stockQuantity);
            cmd.Parameters.AddWithValue("@description", description);
            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        public static List<(int Id, string Name, string Code, int CategoryId, string CategoryName, decimal SalePrice, decimal PurchasePrice, string PurchaseUnit, int ImportQuantity, int StockQuantity, string Description)> GetAllProductsWithCategories()
        {
            var products = new List<(int, string, string, int, string, decimal, decimal, string, int, int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string cmdText = @"SELECT p.Id, p.Name, p.Code, p.CategoryId, c.Name as CategoryName, p.SalePrice, p.PurchasePrice, p.PurchaseUnit, p.ImportQuantity, p.StockQuantity, p.Description 
                              FROM Products p 
                              LEFT JOIN Categories c ON p.CategoryId = c.Id 
                              ORDER BY p.Name
                              LIMIT 10000;";
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
                    reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                    reader.IsDBNull(7) ? "" : reader.GetString(7),
                    reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    reader.GetInt32(9),
                    reader.IsDBNull(10) ? "" : reader.GetString(10)
                ));
            }
            return products;
        }

        public static bool UpdateProduct(int id, string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int importQuantity, int stockQuantity, string description = "")
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string cmdText = "UPDATE Products SET Name=@name, Code=@code, CategoryId=@categoryId, SalePrice=@salePrice, PurchasePrice=@purchasePrice, PurchaseUnit=@purchaseUnit, ImportQuantity=@importQuantity, StockQuantity=@stockQuantity, Description=@description WHERE Id=@id;";
            using var cmd = new MySqlCommand(cmdText, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@salePrice", salePrice);
            cmd.Parameters.AddWithValue("@purchasePrice", purchasePrice);
            cmd.Parameters.AddWithValue("@purchaseUnit", purchaseUnit);
            cmd.Parameters.AddWithValue("@importQuantity", importQuantity);
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

        public static int GetProductStockQuantity(int productId)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string cmd = "SELECT StockQuantity FROM Products WHERE Id = @id;";
            using var check = new MySqlCommand(cmd, connection);
            check.Parameters.AddWithValue("@id", productId);
            var result = check.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public static bool DeleteProduct(int id)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if product is used by any invoices
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
                // Continue with deletion if check fails
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
            catch
            {
                // If normal delete fails, try with foreign key checks disabled
                try
                {

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
                catch
                {
                    return false;
                }
            }
        }

        public static bool DeleteAllProducts()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Check if any products are used by invoices
                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM InvoiceItems;";
                    using var check = new MySqlCommand(checkCmd, connection, tx);
                    long count = (long)check.ExecuteScalar();

                    if (count > 0)
                    {
                        return false; // Products are in use by invoices
                    }
                }
                catch
                {
                    // InvoiceItems table may not exist, continue with truncate
                }

                // Disable foreign key checks
                using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Products table
                using var truncateCmd = new MySqlCommand("TRUNCATE TABLE Products;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection, tx);
                enableFK.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                return false;
            }
        }

        public static int ImportProductsFromCsv(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return -1;

                var lines = System.IO.File.ReadAllLines(filePath);
                if (lines.Length == 0) return 0;

                // Parse header
                string[] header = lines[0].Split(',');
                int idxName = Array.FindIndex(header, h => string.Equals(h.Trim(), "Name", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "TÃƒÂªn", StringComparison.OrdinalIgnoreCase));
                int idxCode = Array.FindIndex(header, h => string.Equals(h.Trim(), "Code", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "MÃƒÂ£", StringComparison.OrdinalIgnoreCase));
                int idxCategoryId = Array.FindIndex(header, h => string.Equals(h.Trim(), "CategoryId", StringComparison.OrdinalIgnoreCase));
                int idxCategoryName = Array.FindIndex(header, h => string.Equals(h.Trim(), "CategoryName", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "DanhMuc", StringComparison.OrdinalIgnoreCase));
                int idxPrice = Array.FindIndex(header, h => string.Equals(h.Trim(), "Price", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "SalePrice", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "GiÃƒÂ¡", StringComparison.OrdinalIgnoreCase));
                int idxPurchasePrice = Array.FindIndex(header, h => string.Equals(h.Trim(), "PurchasePrice", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "ImportPrice", StringComparison.OrdinalIgnoreCase));
                int idxPurchaseUnit = Array.FindIndex(header, h => string.Equals(h.Trim(), "PurchaseUnit", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "Unit", StringComparison.OrdinalIgnoreCase));
                int idxImportQuantity = Array.FindIndex(header, h => string.Equals(h.Trim(), "ImportQuantity", StringComparison.OrdinalIgnoreCase));
                int idxStock = Array.FindIndex(header, h => string.Equals(h.Trim(), "StockQuantity", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "Stock", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "TÃ¡Â»â€œn", StringComparison.OrdinalIgnoreCase));

                int idxDesc = Array.FindIndex(header, h => string.Equals(h.Trim(), "Description", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "MÃƒÂ´TÃ¡ÂºÂ£", StringComparison.OrdinalIgnoreCase));

                if (idxName < 0 || idxPrice < 0)
                {
                    return -1; // required columns missing
                }

                int successCount = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    var raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var cols = SplitCsvLine(raw);

                    string name = SafeGet(cols, idxName);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string code = SafeGet(cols, idxCode);
                    string catName = SafeGet(cols, idxCategoryName);
                    string priceStr = SafeGet(cols, idxPrice);
                    string purchasePriceStr = SafeGet(cols, idxPurchasePrice);
                    string purchaseUnit = SafeGet(cols, idxPurchaseUnit);
                    string importQuantityStr = SafeGet(cols, idxImportQuantity);
                    string stockStr = SafeGet(cols, idxStock);
                    string desc = SafeGet(cols, idxDesc);

                    if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                    {
                        if (!decimal.TryParse(priceStr, out price)) price = 0;
                    }
                    decimal purchasePrice = 0;
                    if (idxPurchasePrice >= 0 && !string.IsNullOrWhiteSpace(purchasePriceStr))
                    {
                        if (!decimal.TryParse(purchasePriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out purchasePrice))
                        {
                            purchasePrice = price * 0.8m; // Default: 80% of sale price
                        }
                    }
                    else
                    {
                        purchasePrice = price * 0.8m; // Default: 80% of sale price
                    }
                    
                    if (!int.TryParse(stockStr, out int stock)) stock = 0;
                    int importQuantity = 0;
                    if (idxImportQuantity >= 0 && !string.IsNullOrWhiteSpace(importQuantityStr))
                    {
                        int.TryParse(importQuantityStr, out importQuantity);
                    }
                    string unitValue = purchaseUnit ?? "";

                    int categoryId = 0;
                    
                    // Prioritize CategoryName over CategoryId for auto-creation
                    if (!string.IsNullOrWhiteSpace(catName))
                    {
                        categoryId = EnsureCategory(catName);
                    }
                    else
                    {
                        // Fallback to CategoryId if CategoryName is empty
                        var catIdStr = SafeGet(cols, idxCategoryId);
                        if (!string.IsNullOrWhiteSpace(catIdStr)) 
                        {
                            int.TryParse(catIdStr, out categoryId);
                        }
                    }

                    try
                    {
                        // Use the overload with PurchasePrice and PurchaseUnit
                        if (AddProduct(name, code ?? string.Empty, categoryId, price, purchasePrice, unitValue, importQuantity, stock, desc ?? string.Empty))
                        {
                            successCount++;
                        }
                    }
                    catch
                    {
                        // Silent failure for product addition
                    }
                }

                return successCount;
            }
            catch
            {
                return -1;
            }
        }

        public static bool ExportProductsToCsv(string filePath)
        {
            try
            {
                var products = GetAllProductsWithCategories();
                using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                // Header - updated to match current database structure with PurchasePrice and PurchaseUnit
                writer.WriteLine("Id,Name,Code,CategoryId,CategoryName,SalePrice,PurchasePrice,PurchaseUnit,ImportQuantity,StockQuantity,Description");
                foreach (var p in products)
                {
                    string line = string.Join(",", new string[]
                    {
                        p.Id.ToString(),
                        EscapeCsvField(p.Name),
                        EscapeCsvField(p.Code),
                        p.CategoryId.ToString(),
                        EscapeCsvField(p.CategoryName),
                        p.SalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        p.PurchasePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        EscapeCsvField(p.PurchaseUnit),
                        p.ImportQuantity.ToString(),
                        p.StockQuantity.ToString(),
                        EscapeCsvField(p.Description)
                    });
                    writer.WriteLine(line);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int EnsureCategory(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return 0;
                
                // Normalize category name
                name = name.Trim();
                
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                try { using var setNames = new MySqlCommand("SET NAMES utf8mb4;", connection); setNames.ExecuteNonQuery(); } catch { }

                // Check if category exists
                using (var getCmd = new MySqlCommand("SELECT Id FROM Categories WHERE Name=@n;", connection))
                {
                    getCmd.Parameters.AddWithValue("@n", name);
                    var idObj = getCmd.ExecuteScalar();
                    if (idObj != null) 
                    {
                        int existingId = Convert.ToInt32(idObj);
                        return existingId;
                    }
                }

                // Create new category
                using (var insCmd = new MySqlCommand("INSERT INTO Categories (Name) VALUES (@n);", connection))
                {
                    insCmd.Parameters.AddWithValue("@n", name);
                    int rowsAffected = insCmd.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        // Get the inserted ID
                        using var lastIdCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", connection);
                        var newIdObj = lastIdCmd.ExecuteScalar();
                        if (newIdObj != null)
                        {
                            int newId = Convert.ToInt32(newIdObj);
                            return newId;
                        }
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string SafeGet(string[] cols, int idx)
        {
            if (idx < 0) return string.Empty;
            return idx < cols.Length ? cols[idx].Trim() : string.Empty;
        }

        private static string EscapeCsvField(string? s)
        {
            s ??= string.Empty;
            if (s.Contains('"')) s = s.Replace("\"", "\"\"");
            if (s.Contains(',') || s.Contains('\n') || s.Contains('\r') || s.Contains('"'))
            {
                s = "\"" + s + "\"";
            }
            return s;
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
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
            List<(int ProductId, int Quantity, decimal UnitPrice)> items,
            DateTime? createdDate = null,
            int? invoiceId = null)  // Thêm tham số để chỉ định ID khi import CSV
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                DateTime invoiceDate = createdDate ?? DateTime.Now;
                int actualInvoiceId;

                if (invoiceId.HasValue)
                {
                    // Import từ CSV với ID cụ thể
                    string insertInvoice = @"INSERT INTO Invoices (Id, CustomerId, EmployeeId, Subtotal, TaxPercent, TaxAmount, Discount, Total, Paid, CreatedDate)
                                             VALUES (@Id, @CustomerId, @EmployeeId, @Subtotal, @TaxPercent, @TaxAmount, @Discount, @Total, @Paid, @CreatedDate);";
                    using var invCmd = new MySqlCommand(insertInvoice, connection, tx);
                    invCmd.Parameters.AddWithValue("@Id", invoiceId.Value);
                    invCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    invCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    invCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    invCmd.Parameters.AddWithValue("@TaxPercent", taxPercent);
                    invCmd.Parameters.AddWithValue("@TaxAmount", taxAmount);
                    invCmd.Parameters.AddWithValue("@Discount", discount);
                    invCmd.Parameters.AddWithValue("@Total", total);
                    invCmd.Parameters.AddWithValue("@Paid", paid);
                    invCmd.Parameters.AddWithValue("@CreatedDate", invoiceDate);
                    invCmd.ExecuteNonQuery();
                    actualInvoiceId = invoiceId.Value;
                }
                else
                {
                    // Tạo hóa đơn mới bình thường (AUTO_INCREMENT)
                    string insertInvoice = @"INSERT INTO Invoices (CustomerId, EmployeeId, Subtotal, TaxPercent, TaxAmount, Discount, Total, Paid, CreatedDate)
                                             VALUES (@CustomerId, @EmployeeId, @Subtotal, @TaxPercent, @TaxAmount, @Discount, @Total, @Paid, @CreatedDate);
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
                    invCmd.Parameters.AddWithValue("@CreatedDate", invoiceDate);
                    var invoiceIdObj = invCmd.ExecuteScalar();
                    actualInvoiceId = Convert.ToInt32(invoiceIdObj);
                }

                foreach (var (productId, quantity, unitPrice) in items)
                {
                    decimal lineTotal = unitPrice * quantity;
                    string insertItem = @"INSERT INTO InvoiceItems (InvoiceId, ProductId, EmployeeId, UnitPrice, Quantity, LineTotal)
                                           VALUES (@InvoiceId, @ProductId, @EmployeeId, @UnitPrice, @Quantity, @LineTotal);";
                    using var itemCmd = new MySqlCommand(insertItem, connection, tx);
                    itemCmd.Parameters.AddWithValue("@InvoiceId", actualInvoiceId);
                    itemCmd.Parameters.AddWithValue("@ProductId", productId);
                    itemCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    itemCmd.Parameters.AddWithValue("@Quantity", quantity);
                    itemCmd.Parameters.AddWithValue("@LineTotal", lineTotal);
                    itemCmd.ExecuteNonQuery();

                    string updateStock = "UPDATE Products SET StockQuantity = GREATEST(0, StockQuantity - @qty) WHERE Id=@pid;";
                    using var stockCmd = new MySqlCommand(updateStock, connection, tx);
                    stockCmd.Parameters.AddWithValue("@qty", quantity);
                    stockCmd.Parameters.AddWithValue("@pid", productId);
                    stockCmd.ExecuteNonQuery();
                }

                tx.Commit();
                LastSavedInvoiceId = actualInvoiceId;
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"SaveInvoice Error: {ex.Message}");
                throw; // Re-throw để caller có thể handle
            }
        }

        public static int LastSavedInvoiceId { get; private set; }

        public static List<(int Id, string Name, string Phone, string Email, string Address, string CustomerType)> GetAllCustomers()
        {
            var customers = new List<(int, string, string, string, string, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Name, Phone, Email, Address, CustomerType FROM Customers ORDER BY Name LIMIT 10000;";
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
            sb.Append(" ORDER BY i.CreatedDate DESC, i.Id DESC LIMIT 10000");

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

            string headerSql = @"SELECT i.Id, i.CreatedDate, c.Name, i.Subtotal, i.TaxPercent, i.TaxAmount, i.Discount, i.Total, i.Paid,
                                        IFNULL(c.Phone, ''), IFNULL(c.Email, ''), IFNULL(c.Address, ''), i.EmployeeId
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
                    TaxPercent = hr.GetDecimal(4),
                    TaxAmount = hr.GetDecimal(5),
                    Discount = hr.GetDecimal(6),
                    Total = hr.GetDecimal(7),
                    Paid = hr.GetDecimal(8),
                    CustomerPhone = hr.IsDBNull(9) ? string.Empty : hr.GetString(9),
                    CustomerEmail = hr.IsDBNull(10) ? string.Empty : hr.GetString(10),
                    CustomerAddress = hr.IsDBNull(11) ? string.Empty : hr.GetString(11),
                    EmployeeId = hr.IsDBNull(12) ? 1 : hr.GetInt32(12)
                };
            }
            else
            {
                return (new InvoiceHeader { Id = invoiceId }, new List<InvoiceItemDetail>());
            }
            hr.Close();

            var items = new List<InvoiceItemDetail>();
            string itemsSql = @"SELECT ii.ProductId, p.Name, ii.UnitPrice, ii.Quantity, ii.LineTotal
                                 FROM InvoiceItems ii
                                 LEFT JOIN Products p ON p.Id = ii.ProductId
                                 WHERE ii.InvoiceId = @id
                                 ORDER BY ii.Id";
            using var icmd = new MySqlCommand(itemsSql, connection);
            icmd.Parameters.AddWithValue("@id", invoiceId);
            using var ir = icmd.ExecuteReader();
            while (ir.Read())
            {
                var item = new InvoiceItemDetail
                {
                    ProductId = ir.GetInt32(0),
                    ProductName = ir.IsDBNull(1) ? "" : ir.GetString(1),
                    UnitPrice = ir.GetDecimal(2),
                    Quantity = ir.GetInt32(3),
                    LineTotal = ir.GetDecimal(4)
                };
                
                System.Diagnostics.Debug.WriteLine($"GetInvoiceDetails: Found item - ProductId: {item.ProductId}, Name: '{item.ProductName}', Qty: {item.Quantity}, Price: {item.UnitPrice:F2}, Total: {item.LineTotal:F2}");
                items.Add(item);
            }
            
            System.Diagnostics.Debug.WriteLine($"GetInvoiceDetails: Invoice {invoiceId} has {items.Count} items");

            return (header, items);
        }

        public class InvoiceHeader
        {
            public int Id { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal Subtotal { get; set; }
            public decimal TaxPercent { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal Discount { get; set; }
            public decimal Total { get; set; }
            public decimal Paid { get; set; }
            public string CustomerPhone { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public string CustomerAddress { get; set; } = string.Empty;
            public int EmployeeId { get; set; }
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
                // First, get invoice items to restore stock
                string getItemsSql = "SELECT ProductId, Quantity FROM InvoiceItems WHERE InvoiceId = @invoiceId";
                var items = new List<(int ProductId, int Quantity)>();
                
                using (var getItemsCmd = new MySqlCommand(getItemsSql, connection, tx))
                {
                    getItemsCmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                    using var reader = getItemsCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        items.Add((reader.GetInt32("ProductId"), reader.GetInt32("Quantity")));
                    }
                }

                // Restore stock for each product
                foreach (var (productId, quantity) in items)
                {
                    string restoreStockSql = "UPDATE Products SET StockQuantity = StockQuantity + @quantity WHERE Id = @productId";
                    using var restoreCmd = new MySqlCommand(restoreStockSql, connection, tx);
                    restoreCmd.Parameters.AddWithValue("@quantity", quantity);
                    restoreCmd.Parameters.AddWithValue("@productId", productId);
                    restoreCmd.ExecuteNonQuery();
                }

                // Delete invoice items first (although CASCADE should handle this)
                using var delItems = new MySqlCommand("DELETE FROM InvoiceItems WHERE InvoiceId = @id", connection, tx);
                delItems.Parameters.AddWithValue("@id", invoiceId);
                delItems.ExecuteNonQuery();

                // Delete invoice
                using var delInvoice = new MySqlCommand("DELETE FROM Invoices WHERE Id = @id", connection, tx);
                delInvoice.Parameters.AddWithValue("@id", invoiceId);
                int affected = delInvoice.ExecuteNonQuery();

                tx.Commit();
                return affected > 0;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                return false;
            }
        }

        public static bool DeleteAllInvoices()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Disable foreign key checks
                using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate InvoiceItems first (child table)
                using var truncateInvoiceItems = new MySqlCommand("TRUNCATE TABLE InvoiceItems;", connection, tx);
                truncateInvoiceItems.ExecuteNonQuery();

                // Truncate Invoices table (parent table)
                using var truncateInvoices = new MySqlCommand("TRUNCATE TABLE Invoices;", connection, tx);
                truncateInvoices.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection, tx);
                enableFK.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                return false;
            }
        }

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

        // Get top selling products by quantity
        public static List<(string ProductName, int Quantity, decimal Revenue)> GetTopProducts(int topN = 10)
        {
            var list = new List<(string, int, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT p.Name, SUM(ii.Quantity) as qty, SUM(ii.LineTotal) as rev
                           FROM InvoiceItems ii
                           JOIN Products p ON p.Id = ii.ProductId
                           GROUP BY p.Name
                           ORDER BY qty DESC
                           LIMIT @top";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@top", topN);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((
                    r.IsDBNull(0) ? "(Unknown)" : r.GetString(0), 
                    r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    r.IsDBNull(2) ? 0m : r.GetDecimal(2)
                ));
            }
            return list;
        }

        public static List<(string CategoryName, decimal Revenue)> GetRevenueByCategory(DateTime from, DateTime to, int topN = 10000)
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

        // Get top customers by total spending
        public static List<(string CustomerName, decimal TotalSpent)> GetTopCustomers(int topN = 10)
        {
            var list = new List<(string, decimal)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT c.Name, SUM(i.Total) as TotalSpent
                           FROM Invoices i
                           LEFT JOIN Customers c ON c.Id = i.CustomerId
                           WHERE c.Name IS NOT NULL
                           GROUP BY c.Name
                           ORDER BY TotalSpent DESC
                           LIMIT @top";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@top", topN);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((r.IsDBNull(0) ? "Unknown" : r.GetString(0), r.IsDBNull(1) ? 0m : r.GetDecimal(1)));
            }
            return list;
        }


        public static List<(string ProductName, int StockQuantity, string CategoryName)> GetLowStockProducts(int threshold = 10)
        {
            var list = new List<(string, int, string)>();
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT p.Name, p.StockQuantity, IFNULL(c.Name, 'Uncategorized') as CategoryName
                           FROM Products p
                           LEFT JOIN Categories c ON c.Id = p.CategoryId
                           WHERE p.StockQuantity <= @threshold
                           ORDER BY p.StockQuantity ASC
                           LIMIT 100";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@threshold", threshold);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add((
                    r.IsDBNull(0) ? "Unknown" : r.GetString(0),
                    r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    r.IsDBNull(2) ? "Uncategorized" : r.GetString(2)
                ));
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

        public static int ImportCustomersFromCsv(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return -1;
                var lines = System.IO.File.ReadAllLines(filePath);
                if (lines.Length == 0) return 0;

                string[] header = lines[0].Split(',');
                int idxName = Array.FindIndex(header, h => string.Equals(h.Trim(), "Name", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "TÃƒÂªn", StringComparison.OrdinalIgnoreCase));
                int idxPhone = Array.FindIndex(header, h => string.Equals(h.Trim(), "Phone", StringComparison.OrdinalIgnoreCase));
                int idxEmail = Array.FindIndex(header, h => string.Equals(h.Trim(), "Email", StringComparison.OrdinalIgnoreCase));
                int idxType = Array.FindIndex(header, h => string.Equals(h.Trim(), "CustomerType", StringComparison.OrdinalIgnoreCase));
                int idxAddress = Array.FindIndex(header, h => string.Equals(h.Trim(), "Address", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "Ã„ÂÃ¡Â»â€¹aChÃ¡Â»â€°", StringComparison.OrdinalIgnoreCase));
                int idxTier = Array.FindIndex(header, h => string.Equals(h.Trim(), "Tier", StringComparison.OrdinalIgnoreCase));
                int idxPoints = Array.FindIndex(header, h => string.Equals(h.Trim(), "Points", StringComparison.OrdinalIgnoreCase));

                if (idxName < 0) return -1;

                int success = 0;
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                for (int i = 1; i < lines.Length; i++)
                {
                    var raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var cols = SplitCsvLine(raw);
                    string name = SafeGet(cols, idxName);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string phone = SafeGet(cols, idxPhone);
                    string email = SafeGet(cols, idxEmail);
                    string type = SafeGet(cols, idxType);
                    if (string.IsNullOrWhiteSpace(type)) type = "Regular";
                    string address = SafeGet(cols, idxAddress);
                    string tier = SafeGet(cols, idxTier);
                    if (string.IsNullOrWhiteSpace(tier)) tier = "Regular";
                    int points = 0; int.TryParse(SafeGet(cols, idxPoints), out points);

                    try
                    {
                        // Upsert by Name+Phone as a basic key (can adjust if needed)
                        using var up = new MySqlCommand(@"INSERT INTO Customers (Name, Phone, Email, CustomerType, Address)
                                                          VALUES (@n, @ph, @em, @t, @ad)
                                                          ON DUPLICATE KEY UPDATE Email=VALUES(Email), CustomerType=VALUES(CustomerType), Address=VALUES(Address);", connection);
                        up.Parameters.AddWithValue("@n", name);
                        up.Parameters.AddWithValue("@ph", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                        up.Parameters.AddWithValue("@em", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                        up.Parameters.AddWithValue("@t", type);
                        up.Parameters.AddWithValue("@ad", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);
                        up.ExecuteNonQuery();

                        // Fetch id
                        int id;
                        using (var getId = new MySqlCommand("SELECT Id FROM Customers WHERE Name=@n AND (Phone <=> @ph);", connection))
                        {
                            getId.Parameters.AddWithValue("@n", name);
                            getId.Parameters.AddWithValue("@ph", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                            id = Convert.ToInt32(getId.ExecuteScalar());
                        }

                        // Update loyalty
                        UpdateCustomerLoyalty(id, points, tier);

                        success++;
                    }
                    catch { }
                }
                return success;
            }
            catch { return -1; }
        }

        public static bool ExportCustomersToCsv(string filePath)
        {
            try
            {
                var customers = GetAllCustomers();
                using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                writer.WriteLine("Id,Name,Phone,Email,CustomerType,Address,Tier,Points");
                foreach (var c in customers)
                {
                    var (tier, pts) = GetCustomerLoyalty(c.Id);
                    writer.WriteLine(string.Join(",", new[]
                    {
                        c.Id.ToString(),
                        EscapeCsvField(c.Name),
                        EscapeCsvField(c.Phone),
                        EscapeCsvField(c.Email),
                        EscapeCsvField(c.CustomerType),
                        EscapeCsvField(c.Address),
                        EscapeCsvField(tier),
                        pts.ToString()
                    }));
                }
                return true;
            }
            catch { return false; }
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

            // Check if customer is used by any invoices
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
                // Continue with deletion if check fails
            }

            string deleteCmd = "DELETE FROM Customers WHERE Id=@id;";
            using var cmd = new MySqlCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteAllCustomers()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Check if any invoices reference customers
                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM Invoices;";
                    using var check = new MySqlCommand(checkCmd, connection, tx);
                    long count = (long)check.ExecuteScalar();
                    if (count > 0)
                    {
                        return false; // There are invoices; refuse hard delete to keep integrity
                    }
                }
                catch
                {
                    // Allow truncate if check fails
                }

                // Disable foreign key checks
                using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Customers table
                using var truncateCmd = new MySqlCommand("TRUNCATE TABLE Customers;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection, tx);
                enableFK.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error truncating customers: {ex.Message}");
                return false;
            }
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

        public static bool DeleteAllCategories()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {

                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM Products WHERE CategoryId > 0;";
                    using var check = new MySqlCommand(checkCmd, connection, tx);
                    long count = (long)check.ExecuteScalar();
                    if (count > 0)
                    {
                        return false;
                    }
                }
                catch
                {

                }
                using var disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Categories table
                using var truncateCmd = new MySqlCommand("TRUNCATE TABLE Categories;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection, tx);
                enableFK.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error truncating categories: {ex.Message}");
                return false;
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
        // Methods for ReportsSettingsWindow
        public static int GetTotalInvoices()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Invoices", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static decimal GetTotalRevenue()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT IFNULL(SUM(Total), 0) FROM Invoices", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToDecimal(val ?? 0);
        }

        public static (DateTime? oldestDate, DateTime? newestDate) GetInvoiceDateRange()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var cmd = new MySqlCommand("SELECT MIN(CreatedDate), MAX(CreatedDate) FROM Invoices", connection);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var oldest = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
                var newest = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                return (oldest, newest);
            }
            return (null, null);
        }


        public static bool ExportInvoicesToCsv(string filePath)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                WriteInvoiceCsvHeader(writer);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                var invoiceIds = GetAllInvoiceIds(connection);
                foreach (int invoiceId in invoiceIds)
                {
                    WriteInvoiceHeaderRecord(writer, connection, invoiceId);
                    WriteInvoiceItemsRecords(writer, connection, invoiceId);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteInvoiceCsvHeader(System.IO.StreamWriter writer)
        {
            writer.WriteLine("InvoiceId,InvoiceDate,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Subtotal,TaxPercent,TaxAmount,Discount,Total,Paid,EmployeeId");
        }

        private static List<int> GetAllInvoiceIds(MySqlConnection connection)
        {
            var invoiceIds = new List<int>();
            string idsSql = "SELECT Id FROM Invoices ORDER BY Id DESC LIMIT 10000";
            using var idsCmd = new MySqlCommand(idsSql, connection);
            using var idsReader = idsCmd.ExecuteReader();
            while (idsReader.Read())
            {
                invoiceIds.Add(idsReader.GetInt32(0));
            }
            idsReader.Close();
            return invoiceIds;
        }

        private static void WriteInvoiceHeaderRecord(System.IO.StreamWriter writer, MySqlConnection connection, int invoiceId)
        {
            string sql = @"SELECT i.Id, i.CreatedDate, c.Name,
                                  IFNULL(c.Phone, ''), IFNULL(c.Email, ''), IFNULL(c.Address, ''),
                                  i.Subtotal, i.TaxPercent, i.TaxAmount, i.Discount, i.Total, i.Paid, i.EmployeeId
                           FROM Invoices i
                           LEFT JOIN Customers c ON c.Id = i.CustomerId
                           WHERE i.Id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", invoiceId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                writer.WriteLine(string.Join(",", new string[]
                {
                    reader.GetInt32(0).ToString(),
                    EscapeCsvField(reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsvField(reader.IsDBNull(2) ? string.Empty : reader.GetString(2)),
                    EscapeCsvField(reader.IsDBNull(3) ? string.Empty : reader.GetString(3)),
                    EscapeCsvField(reader.IsDBNull(4) ? string.Empty : reader.GetString(4)),
                    EscapeCsvField(reader.IsDBNull(5) ? string.Empty : reader.GetString(5)),
                    reader.GetDecimal(6).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetDecimal(7).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetDecimal(8).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetDecimal(9).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetDecimal(10).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetDecimal(11).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetInt32(12).ToString()
                }));
            }
            reader.Close();
        }

        private static void WriteInvoiceItemsRecords(System.IO.StreamWriter writer, MySqlConnection connection, int invoiceId)
        {
            string itemsSql = @"SELECT p.Id, p.Name, ii.Quantity, ii.UnitPrice, ii.LineTotal
                                FROM InvoiceItems ii
                                INNER JOIN Products p ON p.Id = ii.ProductId
                                WHERE ii.InvoiceId = @id";
            using var itemsCmd = new MySqlCommand(itemsSql, connection);
            itemsCmd.Parameters.AddWithValue("@id", invoiceId);
            using var itemsReader = itemsCmd.ExecuteReader();
            while (itemsReader.Read())
            {
                writer.WriteLine(string.Join(",", new string[]
                {
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    "ITEM",
                    EscapeCsvField(itemsReader.IsDBNull(1) ? string.Empty : itemsReader.GetString(1)),
                    itemsReader.GetInt32(0).ToString(),
                    itemsReader.GetInt32(2).ToString(),
                    itemsReader.GetDecimal(3).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    itemsReader.GetDecimal(4).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }));
            }
            itemsReader.Close();
        }

        public static int ImportInvoicesFromCsv(string filePath)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length <= 1) 
                {
                    return 0;
                }


                int successCount = 0;
                var currentInvoice = new InvoiceHeader();
                var currentItems = new List<(int ProductId, int Quantity, decimal UnitPrice)>();

                int employeeId = ResolveEmployeeIdFromCurrentUserOrDefault();

                for (int i = 1; i < lines.Length; i++)
                {
                    var fields = SplitCsvLine(lines[i]);

                    if (fields.Length == 0 || (fields.Length == 1 && string.IsNullOrWhiteSpace(fields[0])))
                    {
                        continue;
                    }
                    
                    // Check if this is an ITEM line (fields[6] = "ITEM")
                    if (fields.Length > 6 && fields[6] == "ITEM")
                    {
                        TryAppendItemFromFields(currentItems, fields);
                    }
                    else if (fields.Length >= 13 && !string.IsNullOrEmpty(fields[0]))
                    {
                        // This is an invoice header
                        // Save previous invoice if exists
                        if (currentInvoice.Id > 0 && currentItems.Count > 0)
                        {
                            if (SaveInvoiceWithResolvedCustomer(currentInvoice, currentItems, employeeId)) successCount++;
                        }
                        else if (currentInvoice.Id > 0 && currentItems.Count == 0)
                        {
                            
                        }

                        if (int.TryParse(fields[0], out int invId) &&
                            DateTime.TryParse(fields[1], out DateTime invDate))
                        {      
                            // Invoice format: InvoiceId,InvoiceDate,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Subtotal,TaxPercent,TaxAmount,Discount,Total,Paid,EmployeeId
                            currentInvoice = new InvoiceHeader
                            {
                                Id = invId,
                                CreatedDate = invDate,
                                CustomerName = fields[2].Trim('"'),
                                CustomerPhone = fields[3].Trim('"'),
                                CustomerEmail = fields[4].Trim('"'),
                                CustomerAddress = fields[5].Trim('"'),
                                Subtotal = decimal.Parse(fields[6]),
                                TaxPercent = decimal.Parse(fields[7]),
                                TaxAmount = decimal.Parse(fields[8]),
                                Discount = decimal.Parse(fields[9]),
                                Total = decimal.Parse(fields[10]),
                                Paid = decimal.Parse(fields[11]),
                                EmployeeId = int.TryParse(fields[12], out int empId) ? empId : employeeId
                            };
                            currentItems.Clear();
                        }
                        else
                        {
                        }
                    }
                }

                if (currentInvoice.Id > 0 && currentItems.Count > 0)
                {
                    if (SaveInvoiceWithResolvedCustomer(currentInvoice, currentItems, employeeId)) successCount++;
                }

                
                // Reset AUTO_INCREMENT để tránh conflict ID trong tương lai
                if (successCount > 0)
                {
                    try
                    {
                        using var connection = new MySqlConnection(ConnectionString);
                        connection.Open();
                        
                        // Lấy ID lớn nhất hiện tại
                        string getMaxIdCmd = "SELECT IFNULL(MAX(Id), 0) FROM Invoices";
                        using var maxIdCmd = new MySqlCommand(getMaxIdCmd, connection);
                        var maxId = Convert.ToInt32(maxIdCmd.ExecuteScalar());
                        
                        // Reset AUTO_INCREMENT về ID lớn nhất + 1
                        string resetAutoIncrement = $"ALTER TABLE Invoices AUTO_INCREMENT = {maxId + 1}";
                        using var resetCmd = new MySqlCommand(resetAutoIncrement, connection);
                        resetCmd.ExecuteNonQuery();
                        
                        System.Diagnostics.Debug.WriteLine($"Reset AUTO_INCREMENT to {maxId + 1} after importing {successCount} invoices");
                    }
                    catch (Exception resetEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not reset AUTO_INCREMENT: {resetEx.Message}");
                        // Không throw - import vẫn thành công
                    }
                }
                
                return successCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImportInvoicesFromCsv Error: {ex.Message}");
                return -1;
            }
        }

        private static int ResolveEmployeeIdFromCurrentUserOrDefault()
        {
            try
            {
                var currentUser = System.Windows.Application.Current?.Resources["CurrentUser"] as string;
                if (!string.IsNullOrEmpty(currentUser))
                {
                    return GetEmployeeIdByUsername(currentUser);
                }
            }
            catch { }
            return 1;
        }

        private static void TryAppendItemFromFields(List<(int ProductId, int Quantity, decimal UnitPrice)> items, string[] fields)
        {
            if (fields.Length < 12) return;
            var productName = fields[7]?.Trim('"') ?? string.Empty;
            bool parsedQty = int.TryParse(fields[9], out int qty);
            bool parsedUnit = decimal.TryParse(fields[10], out decimal unitPrice);
            if (!parsedQty || !parsedUnit) return;

            int productIdToUse = 0;
            if (int.TryParse(fields[8], out int productIdFromCsv))
            {
                if (DoesProductIdExist(productIdFromCsv))
                {
                    productIdToUse = productIdFromCsv;
                }
            }
            if (productIdToUse == 0 && !string.IsNullOrWhiteSpace(productName))
            {
                productIdToUse = FindProductIdByName(productName);
            }
            if (productIdToUse > 0)
            {
                items.Add((productIdToUse, qty, unitPrice));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ImportInvoicesFromCsv: Skipped item for unknown product '{productName}' (csvId='{(fields.Length>8?fields[8]:string.Empty)}')");
            }
        }

        private static bool SaveInvoiceWithResolvedCustomer(InvoiceHeader header, List<(int ProductId, int Quantity, decimal UnitPrice)> items, int defaultEmployeeId)
        {
            try
            {
                var customerId = GetOrCreateCustomerId(header.CustomerName, header.CustomerPhone, header.CustomerEmail, header.CustomerAddress);
                var empId = header.EmployeeId > 0 ? header.EmployeeId : defaultEmployeeId;
                return SaveInvoice(
                    customerId,
                    empId,
                    header.Subtotal,
                    header.TaxPercent,
                    header.TaxAmount,
                    header.Discount,
                    header.Total,
                    header.Paid,
                    items,
                    header.CreatedDate,
                    header.Id
                );
            }
            catch { return false; }
        }


        private static bool DoesProductIdExist(int productId)
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("SELECT 1 FROM Products WHERE Id=@id LIMIT 1;", connection);
                cmd.Parameters.AddWithValue("@id", productId);
                var exists = cmd.ExecuteScalar();
                return exists != null;
            }
            catch
            {
                return false;
            }
        }

        private static int FindProductIdByName(string productName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName)) return 0;
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("SELECT Id FROM Products WHERE Name=@name LIMIT 1;", connection);
                cmd.Parameters.AddWithValue("@name", productName);
                var val = cmd.ExecuteScalar();
                return val == null ? 0 : Convert.ToInt32(val);
            }
            catch
            {
                return 0;
            }
        }
        private static int GetOrCreateCustomerId(string name, string phone, string email, string address)
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Try to find existing customer by phone
            if (!string.IsNullOrWhiteSpace(phone))
            {
                string findCmd = "SELECT Id FROM Customers WHERE Phone=@phone LIMIT 1";
                using var findCmdObj = new MySqlCommand(findCmd, connection);
                findCmdObj.Parameters.AddWithValue("@phone", phone);
                var found = findCmdObj.ExecuteScalar();
                if (found != null)
                {
                    return Convert.ToInt32(found);
                }
            }

            // Create new customer
            string insertCmd = "INSERT INTO Customers (Name, Phone, Email, Address) VALUES (@name, @phone, @email, @address); SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(insertCmd, connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@phone", phone);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@address", address);
            var newId = cmd.ExecuteScalar();
            return Convert.ToInt32(newId);
        }

    }
}

