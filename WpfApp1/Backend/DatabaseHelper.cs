using Microsoft.Data.Sqlite;

namespace WpfApp1
{
    public static class DatabaseHelper
    {
        private static string ConnectionString => SettingsManager.BuildConnectionString();
        

        public static void InitializeDatabase()
        {

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            
            // Enable foreign key constraints
            using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            enableFK.ExecuteNonQuery();

            string tableCmd = @"CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL DEFAULT 'Cashier'
            );";
            using var cmd = new SqliteCommand(tableCmd, connection);
            cmd.ExecuteNonQuery();


            string categoryCmd = @"CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );";
            using var catCmd = new SqliteCommand(categoryCmd, connection);
            catCmd.ExecuteNonQuery();

            string productCmd = @"CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Code TEXT UNIQUE,
                CategoryId INTEGER,
                SalePrice REAL NOT NULL,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                Description TEXT,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );";
            using var prodCmd = new SqliteCommand(productCmd, connection);
            prodCmd.ExecuteNonQuery();

            string customerCmd = @"CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                CustomerType TEXT DEFAULT 'Regular',
                Points INTEGER NOT NULL DEFAULT 0,
                UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
            );";
            using var custCmd = new SqliteCommand(customerCmd, connection);
            custCmd.ExecuteNonQuery();

            try
            {
                using var checkPoints = new SqliteCommand("PRAGMA table_info(Customers);", connection);
                using var reader = checkPoints.ExecuteReader();
                bool pointsExists = false;
                while (reader.Read())
                {
                    if (reader.GetString(1) == "Points")
                    {
                        pointsExists = true;
                        break;
                    }
                }
                reader.Close();
                
                if (!pointsExists)
                {
                    using var addPoints = new SqliteCommand("ALTER TABLE Customers ADD COLUMN Points INTEGER NOT NULL DEFAULT 0;", connection);
                    addPoints.ExecuteNonQuery();
                }
            }
            catch { }

            string invoicesCmd = @"CREATE TABLE IF NOT EXISTS Invoices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                EmployeeId INTEGER NOT NULL,
                Subtotal REAL NOT NULL,
                TaxPercent REAL NOT NULL DEFAULT 0,
                TaxAmount REAL NOT NULL DEFAULT 0,
                Discount REAL NOT NULL DEFAULT 0,
                Total REAL NOT NULL,
                Paid REAL NOT NULL DEFAULT 0,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id)
            );";
            using var invCmd = new SqliteCommand(invoicesCmd, connection);
            invCmd.ExecuteNonQuery();

            string invoiceItemsCmd = @"CREATE TABLE IF NOT EXISTS InvoiceItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                EmployeeId INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                LineTotal REAL NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
                FOREIGN KEY (ProductId) REFERENCES Products(Id),
                FOREIGN KEY (EmployeeId) REFERENCES Accounts(Id)
            );";
            using var invItemsCmd = new SqliteCommand(invoiceItemsCmd, connection);
            invItemsCmd.ExecuteNonQuery();

            UpdateProductsTable(connection);


            FixExistingProductData(connection);

            string checkAdminCmd = "SELECT COUNT(*) FROM Accounts WHERE Username='admin';";
            using var checkCmd = new SqliteCommand(checkAdminCmd, connection);
            long adminExists = (long)checkCmd.ExecuteScalar();
            if (adminExists == 0)
            {
                string insertAdminCmd = "INSERT INTO Accounts (Username, Password, Role) VALUES ('admin', 'admin', 'Admin');";
                using var insertCmd = new SqliteCommand(insertAdminCmd, connection);
                insertCmd.ExecuteNonQuery();
            }
        }

        

        private static void UpdateProductsTable(SqliteConnection connection)
        {
            try
            {
                // Check if Code column exists
                using var checkCode = new SqliteCommand("PRAGMA table_info(Products);", connection);
                using var reader = checkCode.ExecuteReader();
                bool codeExists = false;
                while (reader.Read())
                {
                    if (reader.GetString(1) == "Code")
                    {
                        codeExists = true;
                        break;
                    }
                }
                reader.Close();

                if (!codeExists)
                {
                    // Add Code column
                    string addCodeCmd = "ALTER TABLE Products ADD COLUMN Code TEXT;";
                    using var addCode = new SqliteCommand(addCodeCmd, connection);
                    addCode.ExecuteNonQuery();

                    // Update existing records to have unique codes (SQLite doesn't have LPAD, use printf)
                    string updateCodesCmd = "UPDATE Products SET Code = 'PROD' || printf('%04d', Id) WHERE Code IS NULL OR Code = '';";
                    using var updateCodes = new SqliteCommand(updateCodesCmd, connection);
                    updateCodes.ExecuteNonQuery();

                    // SQLite doesn't support adding UNIQUE constraint to existing column easily
                    // The constraint should be added during table creation
                }

                // Check if Description column exists
                using var checkDesc = new SqliteCommand("PRAGMA table_info(Products);", connection);
                using var reader2 = checkDesc.ExecuteReader();
                bool descExists = false;
                bool createdExists = false;
                bool updatedExists = false;
                
                while (reader2.Read())
                {
                    string columnName = reader2.GetString(1);
                    if (columnName == "Description") descExists = true;
                    if (columnName == "CreatedDate") createdExists = true;
                    if (columnName == "UpdatedDate") updatedExists = true;
                }
                reader2.Close();

                if (!descExists)
                {
                    // Add Description column
                    string addDescCmd = "ALTER TABLE Products ADD COLUMN Description TEXT;";
                    using var addDesc = new SqliteCommand(addDescCmd, connection);
                    addDesc.ExecuteNonQuery();
                }

                if (!createdExists)
                {
                    // Add CreatedDate column
                    string addCreatedCmd = "ALTER TABLE Products ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP;";
                    using var addCreated = new SqliteCommand(addCreatedCmd, connection);
                    addCreated.ExecuteNonQuery();
                }

                if (!updatedExists)
                {
                    // Add UpdatedDate column (SQLite doesn't support ON UPDATE CURRENT_TIMESTAMP)
                    string addUpdatedCmd = "ALTER TABLE Products ADD COLUMN UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP;";
                    using var addUpdated = new SqliteCommand(addUpdatedCmd, connection);
                    addUpdated.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error updating Products table: {ex.Message}");
            }
        }

        private static void FixExistingProductData(SqliteConnection connection)
        {
            try
            {
                // Check if there are any NULL or empty codes and fix them
                string fixCodesCmd = "UPDATE Products SET Code = 'PROD' || printf('%04d', Id) WHERE Code IS NULL OR Code = '';";
                using var fixCodes = new SqliteCommand(fixCodesCmd, connection);
                fixCodes.ExecuteNonQuery();

                // Check for duplicate codes and fix them (SQLite doesn't have RAND(), use random())
                string checkDuplicatesCmd = @"
                    UPDATE Products 
                    SET Code = 'PROD' || printf('%04d', Id) || '_' || (abs(random()) % 1000)
                    WHERE Id IN (
                        SELECT p1.Id FROM Products p1
                        WHERE EXISTS (
                            SELECT 1 FROM Products p2 
                            WHERE p2.Code = p1.Code AND p2.Id != p1.Id
                        )
                    );";
                using var fixDuplicates = new SqliteCommand(checkDuplicatesCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Accounts (Username, EmployeeName, Password, Role) VALUES (@username, @employeeName, @password, @role);";
            using var cmd = new SqliteCommand(insertCmd, connection);
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


        public static string ValidateLogin(string username, string password)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@password;";
            using var cmd = new SqliteCommand(selectCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);
            long count = (long)cmd.ExecuteScalar();
            return count > 0 ? "true" : "false";
        }

        public static string GetUserRole(string username)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Role FROM Accounts WHERE Username=@username;";
            using var cmd = new SqliteCommand(selectCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string verifyCmd = "SELECT COUNT(*) FROM Accounts WHERE Username=@username AND Password=@oldPassword;";
            using var verify = new SqliteCommand(verifyCmd, connection);
            verify.Parameters.AddWithValue("@username", username);
            verify.Parameters.AddWithValue("@oldPassword", oldPassword);
            long count = (long)verify.ExecuteScalar();

            if (count == 0)
                return false;

            string updateCmd = "UPDATE Accounts SET Password=@newPassword WHERE Username=@username;";
            using var update = new SqliteCommand(updateCmd, connection);
            update.Parameters.AddWithValue("@username", username);
            update.Parameters.AddWithValue("@newPassword", newPassword);
            return update.ExecuteNonQuery() > 0;
        }

        public static List<(int Id, string Username, string EmployeeName)> GetAllAccounts()
        {
            var accounts = new List<(int, string, string)>();
            using var connection2 = new SqliteConnection(ConnectionString);
            connection2.Open();
            string selectCmd = "SELECT Id, Username, COALESCE(EmployeeName, '') FROM Accounts;";
            using var cmd2 = new SqliteCommand(selectCmd, connection2);
            using var reader2 = cmd2.ExecuteReader();
            while (reader2.Read())
            {
                accounts.Add((reader2.GetInt32(0), reader2.GetString(1), reader2.IsDBNull(2) ? "" : reader2.GetString(2)));
            }
            return accounts;
        }

        public static int GetEmployeeIdByUsername(string username)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                string selectCmd = "SELECT Id FROM Accounts WHERE Username = @username;";
                using var cmd = new SqliteCommand(selectCmd, connection);
                cmd.Parameters.AddWithValue("@username", username);
                var result = cmd.ExecuteScalar();
                
                if (result != null)
                {
                    int employeeId = Convert.ToInt32(result);
                    System.Diagnostics.Debug.WriteLine($"Found employee ID {employeeId} for username '{username}'");
                    return employeeId;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No employee found for username '{username}', using default ID 1");
                    return 1; // Default to admin ID
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting employee ID for '{username}': {ex.Message}");
                return 1; // Default to admin ID on error
            }
        }

        public static bool DeleteAccount(string username)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string deleteCmd = "DELETE FROM Accounts WHERE Username=@username;";
            using var cmd = new SqliteCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@username", username);
            return cmd.ExecuteNonQuery() > 0;
        }


        public static bool DeleteAllAccountsExceptAdmin()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            // SQLite doesn't need SET NAMES

            try
            {
                string sql = "DELETE FROM Accounts WHERE LOWER(Username) <> 'admin';";
                using var cmd = new SqliteCommand(sql, connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteAllAccountsExceptAdmin error: {ex.Message}");
                return false;
            }
        }


        public static bool AddProduct(string name, string code, int categoryId, decimal salePrice, decimal purchasePrice, string purchaseUnit, int stockQuantity, string description = "")
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if SalePrice column exists using PRAGMA table_info
            using var checkColumn = new SqliteCommand("PRAGMA table_info(Products)", connection);
            using var columnReader = checkColumn.ExecuteReader();
            bool salePriceExists = false;
            while (columnReader.Read())
            {
                if (columnReader.GetString(1) == "SalePrice")
                {
                    salePriceExists = true;
                    break;
                }
            }
            columnReader.Close();

            string cmdText;
            if (salePriceExists)
            {
                cmdText = "INSERT INTO Products (Name, Code, CategoryId, SalePrice, PurchasePrice, PurchaseUnit, StockQuantity, Description) VALUES (@name, @code, @categoryId, @salePrice, @purchasePrice, @purchaseUnit, @stockQuantity, @description);";
                using var cmd = new SqliteCommand(cmdText, connection);
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
                cmdText = "INSERT INTO Products (Name, Code, CategoryId, Price, StockQuantity, Description) VALUES (@name, @code, @categoryId, @price, @stockQuantity, @description);";
                using var cmd = new SqliteCommand(cmdText, connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@price", salePrice);
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
            // Overload method Ã„â€˜Ã¡Â»Æ’ tÃ†Â°Ã†Â¡ng thÃƒÂ­ch ngÃ†Â°Ã¡Â»Â£c - giÃ¡ÂºÂ£ Ã„â€˜Ã¡Â»â€¹nh purchasePrice = price * 0.8
            return AddProduct(name, code, categoryId, price, Math.Round(price * 0.8m, 2), "Piece", stockQuantity, description);
        }

 
 

        public static List<(int Id, string Name, string Code, int CategoryId, decimal SalePrice, int StockQuantity, string Description)> GetAllProducts()
        {
            var products = new List<(int, string, string, int, decimal, int, string)>();
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if SalePrice column exists using PRAGMA table_info
            using var checkColumn = new SqliteCommand("PRAGMA table_info(Products)", connection);
            using var columnReader = checkColumn.ExecuteReader();
            bool salePriceExists = false;
            while (columnReader.Read())
            {
                if (columnReader.GetString(1) == "SalePrice")
                {
                    salePriceExists = true;
                    break;
                }
            }
            columnReader.Close();

            string cmdText;
            if (salePriceExists)
            {
                cmdText = "SELECT Id, Name, Code, CategoryId, SalePrice, StockQuantity, Description FROM Products ORDER BY Name;";
            }
            else
            {
                cmdText = "SELECT Id, Name, Code, CategoryId, Price, StockQuantity, Description FROM Products ORDER BY Name;";
            }

            using var cmd = new SqliteCommand(cmdText, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string cmdText = @"SELECT p.Id, p.Name, p.Code, p.CategoryId, c.Name as CategoryName, p.SalePrice, p.StockQuantity, p.Description 
                              FROM Products p 
                              LEFT JOIN Categories c ON p.CategoryId = c.Id 
                              ORDER BY p.Name;";
            using var cmd = new SqliteCommand(cmdText, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string cmdText = "UPDATE Products SET Name=@name, Code=@code, CategoryId=@categoryId, SalePrice=@saleprice, StockQuantity=@stockQuantity, Description=@description WHERE Id=@id;";
            using var cmd = new SqliteCommand(cmdText, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if product is used by any invoices (when we implement them)
            try
            {
                string checkCmd = "SELECT COUNT(*) FROM InvoiceItems WHERE ProductId=@id;";
                using var check = new SqliteCommand(checkCmd, connection);
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
                using var cmd = new SqliteCommand(cmdText, connection);
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
                    using var disableFK = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection);
                    disableFK.ExecuteNonQuery();

                    // Try delete again
                    string cmdText = "DELETE FROM Products WHERE Id=@id;";
                    using var cmd = new SqliteCommand(cmdText, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    int result = cmd.ExecuteNonQuery();

                    // Re-enable foreign key checks
                    using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Check if any products are used by invoices
                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM InvoiceItems;";
                    using var check = new SqliteCommand(checkCmd, connection, tx);
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
                using var disableFK = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Products table
                using var truncateCmd = new SqliteCommand("DELETE FROM Products;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Reset auto-increment counter
                using var resetCmd = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='Products';", connection, tx);
                resetCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection, tx);
                enableFK.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error truncating products: {ex.Message}");
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
                int idxStock = Array.FindIndex(header, h => string.Equals(h.Trim(), "StockQuantity", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "Stock", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "TÃ¡Â»â€œn", StringComparison.OrdinalIgnoreCase));
                int idxDesc = Array.FindIndex(header, h => string.Equals(h.Trim(), "Description", StringComparison.OrdinalIgnoreCase) || string.Equals(h.Trim(), "MÃƒÂ´TÃ¡ÂºÂ£", StringComparison.OrdinalIgnoreCase));

                if (idxName < 0 || idxPrice < 0)
                {
                    return -1;
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
                    string stockStr = SafeGet(cols, idxStock);
                    string desc = SafeGet(cols, idxDesc);

                    if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                    {
                        if (!decimal.TryParse(priceStr, out price)) price = 0;
                    }
                    if (!int.TryParse(stockStr, out int stock)) stock = 0;

                    int categoryId = 0;
                    
                    // Prioritize CategoryName over CategoryId for auto-creation
                    if (!string.IsNullOrWhiteSpace(catName))
                    {
                        System.Diagnostics.Debug.WriteLine($"ImportCSV: Processing category '{catName}' for product '{name}'");
                        categoryId = EnsureCategory(catName);
                        System.Diagnostics.Debug.WriteLine($"ImportCSV: Category '{catName}' resolved to ID {categoryId}");
                    }
                    else
                    {
                        // Fallback to CategoryId if CategoryName is empty
                        var catIdStr = SafeGet(cols, idxCategoryId);
                        if (!string.IsNullOrWhiteSpace(catIdStr)) 
                        {
                            int.TryParse(catIdStr, out categoryId);
                            System.Diagnostics.Debug.WriteLine($"ImportCSV: Using CategoryId {categoryId} for product '{name}'");
                        }
                    }

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"ImportCSV: Adding product '{name}' with categoryId {categoryId}");
                        if (AddProduct(name, code ?? string.Empty, categoryId, price, stock, desc ?? string.Empty))
                        {
                            successCount++;
                            System.Diagnostics.Debug.WriteLine($"ImportCSV: Successfully added product '{name}'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ImportCSV: Failed to add product '{name}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ImportCSV: Exception adding product '{name}': {ex.Message}");
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
                // Header
                writer.WriteLine("Id,Name,Code,CategoryId,CategoryName,Price,StockQuantity,Description");
                foreach (var p in products)
                {
                    // Escape commas and quotes in text fields
                    string Esc(string? s)
                    {
                        s ??= string.Empty;
                        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                        if (s.Contains(',') || s.Contains('\n') || s.Contains('\r') || s.Contains('"'))
                        {
                            s = "\"" + s + "\"";
                        }
                        return s;
                    }

                    string line = string.Join(",", new string[]
                    {
                        p.Id.ToString(),
                        Esc(p.Name),
                        Esc(p.Code),
                        p.CategoryId.ToString(),
                        Esc(p.CategoryName),
                        p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        p.StockQuantity.ToString(),
                        Esc(p.Description)
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
                
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                // SQLite doesn't need SET NAMES

                System.Diagnostics.Debug.WriteLine($"EnsureCategory: Checking for category '{name}'");

                // Check if category exists
                using (var getCmd = new SqliteCommand("SELECT Id FROM Categories WHERE Name=@n;", connection))
                {
                    getCmd.Parameters.AddWithValue("@n", name);
                    var idObj = getCmd.ExecuteScalar();
                    if (idObj != null) 
                    {
                        int existingId = Convert.ToInt32(idObj);
                        System.Diagnostics.Debug.WriteLine($"EnsureCategory: Found existing category '{name}' with ID {existingId}");
                        return existingId;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"EnsureCategory: Creating new category '{name}'");

                // Create new category
                using (var insCmd = new SqliteCommand("INSERT INTO Categories (Name) VALUES (@n);", connection))
                {
                    insCmd.Parameters.AddWithValue("@n", name);
                    int rowsAffected = insCmd.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        // Get the inserted ID
                        using var lastIdCmd = new SqliteCommand("SELECT last_insert_rowid();", connection);
                        var newIdObj = lastIdCmd.ExecuteScalar();
                        if (newIdObj != null)
                        {
                            int newId = Convert.ToInt32(newIdObj);
                            System.Diagnostics.Debug.WriteLine($"EnsureCategory: Created new category '{name}' with ID {newId}");
                            return newId;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"EnsureCategory: Failed to create category '{name}'");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureCategory error for '{name}': {ex.Message}");
                return 0;
            }
        }

        private static string SafeGet(string[] cols, int idx)
        {
            if (idx < 0) return string.Empty;
            return idx < cols.Length ? cols[idx].Trim() : string.Empty;
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
            List<(int ProductId, int Quantity, decimal UnitPrice)> items)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            
            // Ensure foreign key constraints are enabled
            using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            enableFK.ExecuteNonQuery();
            using var tx = connection.BeginTransaction();
            try
            {
                // Validate foreign keys before inserting
                // Check if customer exists
                using var checkCustomer = new SqliteCommand("SELECT COUNT(*) FROM Customers WHERE Id = @customerId", connection, tx);
                checkCustomer.Parameters.AddWithValue("@customerId", customerId);
                if (Convert.ToInt32(checkCustomer.ExecuteScalar()) == 0)
                {
                    throw new Exception($"Customer with ID {customerId} does not exist");
                }

                // Check if employee exists
                using var checkEmployee = new SqliteCommand("SELECT COUNT(*) FROM Accounts WHERE Id = @employeeId", connection, tx);
                checkEmployee.Parameters.AddWithValue("@employeeId", employeeId);
                if (Convert.ToInt32(checkEmployee.ExecuteScalar()) == 0)
                {
                    throw new Exception($"Employee with ID {employeeId} does not exist");
                }

                // Validate all products exist
                foreach (var (productId, _, _) in items)
                {
                    using var checkProduct = new SqliteCommand("SELECT COUNT(*) FROM Products WHERE Id = @productId", connection, tx);
                    checkProduct.Parameters.AddWithValue("@productId", productId);
                    if (Convert.ToInt32(checkProduct.ExecuteScalar()) == 0)
                    {
                        throw new Exception($"Product with ID {productId} does not exist");
                    }
                }

                string insertInvoice = @"INSERT INTO Invoices (CustomerId, EmployeeId, Subtotal, TaxPercent, TaxAmount, Discount, Total, Paid)
                                         VALUES (@CustomerId, @EmployeeId, @Subtotal, @TaxPercent, @TaxAmount, @Discount, @Total, @Paid);
                                         SELECT last_insert_rowid();";
                using var invCmd = new SqliteCommand(insertInvoice, connection, tx);
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

                foreach (var (productId, quantity, unitPrice) in items)
                {
                    decimal lineTotal = unitPrice * quantity;
                    string insertItem = @"INSERT INTO InvoiceItems (InvoiceId, ProductId, EmployeeId, UnitPrice, Quantity, LineTotal)
                                           VALUES (@InvoiceId, @ProductId, @EmployeeId, @UnitPrice, @Quantity, @LineTotal);";
                    using var itemCmd = new SqliteCommand(insertItem, connection, tx);
                    itemCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    itemCmd.Parameters.AddWithValue("@ProductId", productId);
                    itemCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    itemCmd.Parameters.AddWithValue("@Quantity", quantity);
                    itemCmd.Parameters.AddWithValue("@LineTotal", lineTotal);
                    itemCmd.ExecuteNonQuery();

                    string updateStock = "UPDATE Products SET StockQuantity = MAX(0, StockQuantity - @qty) WHERE Id=@pid;";
                    using var stockCmd = new SqliteCommand(updateStock, connection, tx);
                    stockCmd.Parameters.AddWithValue("@qty", quantity);
                    stockCmd.Parameters.AddWithValue("@pid", productId);
                    stockCmd.ExecuteNonQuery();
                }

                tx.Commit();
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

        public static int LastSavedInvoiceId { get; private set; }

        public static List<(int Id, string Name, string Phone, string Email, string Address, string CustomerType)> GetAllCustomers()
        {
            var customers = new List<(int, string, string, string, string, string)>();
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Name, Phone, Email, Address, CustomerType FROM Customers ORDER BY Name;";
            using var cmd = new SqliteCommand(selectCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT IFNULL(CustomerType,'Regular'), IFNULL(Points,0) FROM Customers WHERE Id=@id;";
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = "UPDATE Customers SET Points=@p, CustomerType=@tier WHERE Id=@id;";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@p", newPoints);
            cmd.Parameters.AddWithValue("@tier", newTier);
            cmd.Parameters.AddWithValue("@id", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static decimal GetRevenueBetween(DateTime from, DateTime to)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT IFNULL(SUM(Total), 0) FROM Invoices WHERE CreatedDate BETWEEN @from AND @to";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            var val = cmd.ExecuteScalar();
            return Convert.ToDecimal(val ?? 0);
        }

        public static int GetInvoiceCountBetween(DateTime from, DateTime to)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = "SELECT COUNT(*) FROM Invoices WHERE CreatedDate BETWEEN @from AND @to";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static int GetTotalCustomers()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM Customers", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static int GetTotalProducts()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM Products", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }


        public static List<(int Id, DateTime CreatedDate, string CustomerName, decimal Subtotal, decimal TaxAmount, decimal Discount, decimal Total, decimal Paid)>
            QueryInvoices(DateTime? from, DateTime? to, int? customerId, string search)
        {
            var list = new List<(int, DateTime, string, decimal, decimal, decimal, decimal, decimal)>();
            using var connection = new SqliteConnection(ConnectionString);
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

            using var cmd = new SqliteCommand(sb.ToString(), connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string headerSql = @"SELECT i.Id, i.CreatedDate, c.Name, i.Subtotal, i.TaxAmount, i.Discount, i.Total, i.Paid,
                                        IFNULL(c.Phone, ''), IFNULL(c.Email, ''), IFNULL(c.Address, ''), i.EmployeeId
                                 FROM Invoices i
                                 LEFT JOIN Customers c ON c.Id = i.CustomerId
                                 WHERE i.Id = @id";
            using var hcmd = new SqliteCommand(headerSql, connection);
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
                    Paid = hr.GetDecimal(7),
                    CustomerPhone = hr.IsDBNull(8) ? string.Empty : hr.GetString(8),
                    CustomerEmail = hr.IsDBNull(9) ? string.Empty : hr.GetString(9),
                    CustomerAddress = hr.IsDBNull(10) ? string.Empty : hr.GetString(10),
                    EmployeeId = hr.IsDBNull(11) ? 1 : hr.GetInt32(11)
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
                                 WHERE ii.InvoiceId = @id";
            using var icmd = new SqliteCommand(itemsSql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // First, get invoice items to restore stock
                string getItemsSql = "SELECT ProductId, Quantity FROM InvoiceItems WHERE InvoiceId = @invoiceId";
                var items = new List<(int ProductId, int Quantity)>();
                
                using (var getItemsCmd = new SqliteCommand(getItemsSql, connection, tx))
                {
                    getItemsCmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                    using var reader = getItemsCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        items.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }

                // Restore stock for each product
                foreach (var (productId, quantity) in items)
                {
                    string restoreStockSql = "UPDATE Products SET StockQuantity = StockQuantity + @quantity WHERE Id = @productId";
                    using var restoreCmd = new SqliteCommand(restoreStockSql, connection, tx);
                    restoreCmd.Parameters.AddWithValue("@quantity", quantity);
                    restoreCmd.Parameters.AddWithValue("@productId", productId);
                    restoreCmd.ExecuteNonQuery();
                }

                // Delete invoice items first (although CASCADE should handle this)
                using var delItems = new SqliteCommand("DELETE FROM InvoiceItems WHERE InvoiceId = @id", connection, tx);
                delItems.Parameters.AddWithValue("@id", invoiceId);
                delItems.ExecuteNonQuery();

                // Delete invoice
                using var delInvoice = new SqliteCommand("DELETE FROM Invoices WHERE Id = @id", connection, tx);
                delInvoice.Parameters.AddWithValue("@id", invoiceId);
                int affected = delInvoice.ExecuteNonQuery();

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

        public static bool DeleteAllInvoices()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting DeleteAllInvoices...");

                // Disable foreign key checks
                using var disableFK = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection, tx);
                disableFK.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Disabled foreign key checks");

                // Truncate InvoiceItems first (child table)
                using var truncateInvoiceItems = new SqliteCommand("DELETE FROM InvoiceItems;", connection, tx);
                truncateInvoiceItems.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Truncated InvoiceItems table");

                // Truncate Invoices table (parent table)
                using var truncateInvoices = new SqliteCommand("DELETE FROM Invoices;", connection, tx);
                truncateInvoices.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Truncated Invoices table");

                // Reset auto-increment counters
                using var resetInvoiceItems = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='InvoiceItems';", connection, tx);
                resetInvoiceItems.ExecuteNonQuery();
                using var resetInvoices = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='Invoices';", connection, tx);
                resetInvoices.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Reset auto-increment counters");

                // Re-enable foreign key checks
                using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection, tx);
                enableFK.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Re-enabled foreign key checks");

                tx.Commit();
                System.Diagnostics.Debug.WriteLine("DeleteAllInvoices completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                System.Diagnostics.Debug.WriteLine($"Error in DeleteAllInvoices: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public static List<(DateTime Day, decimal Revenue)> GetRevenueByDay(DateTime from, DateTime to)
        {
            var list = new List<(DateTime, decimal)>();
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT DATE(CreatedDate) as d, SUM(Total) as revenue
                           FROM Invoices
                           WHERE CreatedDate BETWEEN @from AND @to
                           GROUP BY DATE(CreatedDate)
                           ORDER BY d";
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string sql = @"SELECT p.Name, SUM(ii.Quantity) as qty
                           FROM InvoiceItems ii
                           JOIN Invoices i ON i.Id = ii.InvoiceId
                           LEFT JOIN Products p ON p.Id = ii.ProductId
                           WHERE i.CreatedDate BETWEEN @from AND @to
                           GROUP BY p.Name
                           ORDER BY qty DESC
                           LIMIT @top";
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
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
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Customers (Name, Phone, Email, CustomerType, Address) VALUES (@name, @phone, @email, @customerType, @address);";
            using var cmd = new SqliteCommand(insertCmd, connection);
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
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                
                // Ensure foreign key constraints are enabled
                using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                enableFK.ExecuteNonQuery();
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
                        // Check if customer already exists
                        int existingId = 0;
                        using (var checkCmd = new SqliteCommand("SELECT Id FROM Customers WHERE Name=@n AND (Phone IS @ph OR (Phone IS NULL AND @ph IS NULL));", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@n", name);
                            checkCmd.Parameters.AddWithValue("@ph", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                            var result = checkCmd.ExecuteScalar();
                            if (result != null)
                                existingId = Convert.ToInt32(result);
                        }

                        int customerId;
                        if (existingId > 0)
                        {
                            // Update existing customer
                            using var updateCmd = new SqliteCommand(@"UPDATE Customers SET Email=@em, CustomerType=@t, Address=@ad WHERE Id=@id;", connection);
                            updateCmd.Parameters.AddWithValue("@em", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                            updateCmd.Parameters.AddWithValue("@t", type);
                            updateCmd.Parameters.AddWithValue("@ad", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);
                            updateCmd.Parameters.AddWithValue("@id", existingId);
                            updateCmd.ExecuteNonQuery();
                            customerId = existingId;
                        }
                        else
                        {
                            // Insert new customer
                            using var insertCmd = new SqliteCommand(@"INSERT INTO Customers (Name, Phone, Email, CustomerType, Address)
                                                                     VALUES (@n, @ph, @em, @t, @ad);", connection);
                            insertCmd.Parameters.AddWithValue("@n", name);
                            insertCmd.Parameters.AddWithValue("@ph", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                            insertCmd.Parameters.AddWithValue("@em", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                            insertCmd.Parameters.AddWithValue("@t", type);
                            insertCmd.Parameters.AddWithValue("@ad", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);
                            insertCmd.ExecuteNonQuery();
                            
                            // Get the new customer ID
                            using var getIdCmd = new SqliteCommand("SELECT last_insert_rowid();", connection);
                            customerId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                        }

                        // Update loyalty
                        UpdateCustomerLoyalty(customerId, points, tier);

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
                    string Esc(string? s)
                    {
                        s ??= string.Empty;
                        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                        if (s.Contains(',') || s.Contains('\n') || s.Contains('\r') || s.Contains('"')) s = "\"" + s + "\"";
                        return s;
                    }
                    writer.WriteLine(string.Join(",", new[]
                    {
                        c.Id.ToString(),
                        Esc(c.Name),
                        Esc(c.Phone),
                        Esc(c.Email),
                        Esc(c.CustomerType),
                        Esc(c.Address),
                        Esc(tier),
                        pts.ToString()
                    }));
                }
                return true;
            }
            catch { return false; }
        }

        public static bool UpdateCustomer(int id, string name, string phone, string email, string customerType, string address)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string updateCmd = "UPDATE Customers SET Name=@name, Phone=@phone, Email=@email, CustomerType=@customerType, Address=@address WHERE Id=@id;";
            using var cmd = new SqliteCommand(updateCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if customer is used by any invoices (when we implement them)
            try
            {
                string checkCmd = "SELECT COUNT(*) FROM Invoices WHERE CustomerId=@id;";
                using var check = new SqliteCommand(checkCmd, connection);
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
            using var cmd = new SqliteCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteAllCustomers()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Check if any invoices reference customers
                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM Invoices;";
                    using var check = new SqliteCommand(checkCmd, connection, tx);
                    long count = (long)check.ExecuteScalar();
                    if (count > 0)
                    {
                        return false; // There are invoices; refuse hard delete to keep integrity
                    }
                }
                catch
                {
                    // Invoices table may not exist; allow truncate
                }

                // Disable foreign key checks
                using var disableFK = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Customers table
                using var truncateCmd = new SqliteCommand("DELETE FROM Customers;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Reset auto-increment counter
                using var resetCmd = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='Customers';", connection, tx);
                resetCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection, tx);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string selectCmd = "SELECT Id, Name FROM Categories ORDER BY Name;";
            using var cmd = new SqliteCommand(selectCmd, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add((reader.GetInt32(0), reader.GetString(1)));
            }
            return categories;
        }

        public static bool AddCategory(string name)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string insertCmd = "INSERT INTO Categories (Name) VALUES (@name);";
            using var cmd = new SqliteCommand(insertCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            string updateCmd = "UPDATE Categories SET Name=@name WHERE Id=@id;";
            using var cmd = new SqliteCommand(updateCmd, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if category is used by any products
            string checkCmd = "SELECT COUNT(*) FROM Products WHERE CategoryId=@id;";
            using var check = new SqliteCommand(checkCmd, connection);
            check.Parameters.AddWithValue("@id", id);
            long count = (long)check.ExecuteScalar();

            if (count > 0)
            {
                return false; // Category is in use
            }

            string deleteCmd = "DELETE FROM Categories WHERE Id=@id;";
            using var cmd = new SqliteCommand(deleteCmd, connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteAllCategories()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {

                try
                {
                    string checkCmd = "SELECT COUNT(*) FROM Products WHERE CategoryId > 0;";
                    using var check = new SqliteCommand(checkCmd, connection, tx);
                    long count = (long)check.ExecuteScalar();
                    if (count > 0)
                    {
                        return false; // Categories are in use by products
                    }
                }
                catch
                {

                }
                // Disable foreign key checks
                using var disableFK = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection, tx);
                disableFK.ExecuteNonQuery();

                // Truncate Categories table
                using var truncateCmd = new SqliteCommand("DELETE FROM Categories;", connection, tx);
                truncateCmd.ExecuteNonQuery();

                // Reset auto-increment counter
                using var resetCmd = new SqliteCommand("DELETE FROM sqlite_sequence WHERE name='Categories';", connection, tx);
                resetCmd.ExecuteNonQuery();

                // Re-enable foreign key checks
                using var enableFK = new SqliteCommand("PRAGMA foreign_keys = ON;", connection, tx);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            // SQLite doesn't need SET NAMES

            string sql = @"
                SELECT i.Id, IFNULL(i.CreatedDate, datetime('now')) AS CreatedAt,
                       (SELECT COUNT(*) FROM InvoiceItems ii WHERE ii.InvoiceId = i.Id) AS ItemCount,
                       i.Total
                FROM Invoices i
                WHERE i.CustomerId = @cid
                ORDER BY i.CreatedDate DESC, i.Id DESC;";
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            // SQLite doesn't need SET NAMES

            string sql = @"
                SELECT p.Name AS ProductName, ii.Quantity, ii.UnitPrice, (ii.UnitPrice * ii.Quantity) AS LineTotal
                FROM InvoiceItems ii
                LEFT JOIN Products p ON p.Id = ii.ProductId
                WHERE ii.InvoiceId = @invoiceId;";
            using var cmd = new SqliteCommand(sql, connection);
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
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            // SQLite doesn't need SET NAMES

            var sets = new List<string>();
            using var cmd = new SqliteCommand();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateAccount error: {ex.Message}");
                return false;
            }
        }
        // Methods for ReportsSettingsWindow
        public static int GetTotalInvoices()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM Invoices", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToInt32(val ?? 0);
        }

        public static decimal GetTotalRevenue()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = new SqliteCommand("SELECT IFNULL(SUM(Total), 0) FROM Invoices", connection);
            var val = cmd.ExecuteScalar();
            return Convert.ToDecimal(val ?? 0);
        }

        public static (DateTime? oldestDate, DateTime? newestDate) GetInvoiceDateRange()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using var cmd = new SqliteCommand("SELECT MIN(CreatedDate), MAX(CreatedDate) FROM Invoices", connection);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var oldest = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
                var newest = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                return (oldest, newest);
            }
            return (null, null);
        }

        public static bool BackupDatabase(string filePath)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                var backup = new System.Text.StringBuilder();
                backup.AppendLine("-- Database Backup Created: " + DateTime.Now);
                backup.AppendLine("-- This is a simplified backup for demonstration");
                System.IO.File.WriteAllText(filePath, backup.ToString());
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup error: {ex.Message}");
                return false;
            }
        }

        public static bool RestoreDatabase(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Restore error: {ex.Message}");
                return false;
            }
        }

        public static int DeleteInvoicesOlderThan(DateTime cutoffDate)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                using var detailsCmd = new SqliteCommand("DELETE FROM InvoiceDetails WHERE InvoiceId IN (SELECT Id FROM Invoices WHERE CreatedDate < @cutoff)", connection);
                detailsCmd.Parameters.AddWithValue("@cutoff", cutoffDate);
                detailsCmd.ExecuteNonQuery();
                using var invoicesCmd = new SqliteCommand("DELETE FROM Invoices WHERE CreatedDate < @cutoff", connection);
                invoicesCmd.Parameters.AddWithValue("@cutoff", cutoffDate);
                return invoicesCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete old invoices error: {ex.Message}");
                return 0;
            }
        }

        public static bool OptimizeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                string[] tables = { "Invoices", "InvoiceDetails", "Products", "Categories", "Customers", "Accounts" };
                // SQLite uses VACUUM instead of OPTIMIZE TABLE
                using var cmd = new SqliteCommand("VACUUM;", connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Optimize database error: {ex.Message}");
                return false;
            }
        }


        public static bool ResetAllIdCounters()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                
                // Reset auto-increment counters cho tất cả bảng
                string[] tables = { "Accounts", "Products", "Categories", "Customers", "Invoices", "InvoiceItems" };
                
                foreach (string table in tables)
                {
                    using var cmd = new SqliteCommand($"DELETE FROM sqlite_sequence WHERE name='{table}';", connection);
                    cmd.ExecuteNonQuery();
                }
                
                System.Diagnostics.Debug.WriteLine("All ID counters have been reset to 1");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset ID counters error: {ex.Message}");
                return false;
            }
        }


    }
}

