using Microsoft.Data.Sqlite;
using PhoneMaster.Core.Models;
using System;
using System.Globalization;
using System.IO;

namespace PhoneMaster.Core.Services
{
    public static class DatabaseManager
    {
        private static readonly string dataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                         "PhoneMaster", "Data");

        private static readonly string dbPath =
            Path.Combine(dataFolder, "phonemaster.db");

        // database included with the published app
        private static readonly string bundledDbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "phonemaster.db");

        private static readonly string bundledImagesFolder =
           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PhoneImages");

        public static string ImagesFolder =>
            Path.Combine(dataFolder, "PhoneImages");

        public static string ConnectionString => $"Data Source={dbPath}";

       

        public static void InitializeDatabase()
        {
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory(ImagesFolder);

            if (!File.Exists(dbPath))
            {
                if (File.Exists(bundledDbPath))
                {
                    File.Copy(bundledDbPath, dbPath);
                }
                else
                {
                    throw new FileNotFoundException(
                        "Bundled database file was not found.",
                        bundledDbPath);
                }
            }

            CopyBundledImagesIfMissing();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string createPhonesTable = @"
    CREATE TABLE IF NOT EXISTS Phones (
        PhoneID TEXT PRIMARY KEY,
        Manufacturer TEXT NOT NULL,
        Model TEXT NOT NULL,
        Storage INTEGER NOT NULL,
        ReleaseYear INTEGER NOT NULL,
        Price REAL NOT NULL,
        Stock INTEGER NOT NULL,
        ImageFileName TEXT
    );";

            string createTransactionsTable = @"
    CREATE TABLE IF NOT EXISTS Transactions (
        OrderID TEXT PRIMARY KEY,
        Date TEXT NOT NULL,
        Client TEXT NOT NULL,
        PhoneID TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Quantity INTEGER NOT NULL,
        Contract TEXT NOT NULL,
        Subtotal REAL NOT NULL,
        DiscountPercent REAL NOT NULL,
        DiscountAmount REAL NOT NULL,
        TotalPaid REAL NOT NULL,
        Payment TEXT,
        ProcessedBy TEXT
    );";

            string createClientsTable = @"
    CREATE TABLE IF NOT EXISTS Clients (
        ClientID INTEGER PRIMARY KEY AUTOINCREMENT,
        ClientType TEXT NOT NULL,
        Name TEXT NOT NULL,
        VAT TEXT,
        Email TEXT,
        ContactPhone TEXT,
        Address TEXT,
        Postcode TEXT,
        Town TEXT
    );";

            string createInventoryLogsTable = @"
    CREATE TABLE IF NOT EXISTS InventoryLogs (
        LogID INTEGER PRIMARY KEY AUTOINCREMENT,
        Timestamp TEXT NOT NULL,
        PerformedBy TEXT NOT NULL,
        Action TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Details TEXT NOT NULL
    );";

            string[] commands =
            {
        createPhonesTable,
        createTransactionsTable,
        createClientsTable,
        createInventoryLogsTable
    };

            foreach (string sql in commands)
            {
                using var command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
            }

            EnsureImageFileNameColumnExists(connection);
        }

        private static void EnsureImageFileNameColumnExists(SqliteConnection connection)
        {
            using var checkCommand = new SqliteCommand("PRAGMA table_info(Phones);", connection);
            using var reader = checkCommand.ExecuteReader();

            bool columnExists = false;

            while (reader.Read())
            {
                string columnName = reader["name"]?.ToString() ?? "";
                if (columnName == "ImageFileName")
                {
                    columnExists = true;
                    break;
                }
            }

            if (!columnExists)
            {
                using var alterCommand = new SqliteCommand(
                    "ALTER TABLE Phones ADD COLUMN ImageFileName TEXT;",
                    connection);
                alterCommand.ExecuteNonQuery();
            }
        }

        private static void CopyBundledImagesIfMissing()
        {
            if (!Directory.Exists(bundledImagesFolder))
                return;

            Directory.CreateDirectory(ImagesFolder);

            foreach (string sourceFile in Directory.GetFiles(bundledImagesFolder))
            {
                string fileName = Path.GetFileName(sourceFile);
                string destinationFile = Path.Combine(ImagesFolder, fileName);

                if (!File.Exists(destinationFile))
                {
                    File.Copy(sourceFile, destinationFile);
                }
            }
        }

        // Load Phones
        public static List<Phone> LoadPhones()
        {
            var phones = new List<Phone>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM Phones";

            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                phones.Add(new Phone(
                    reader["PhoneID"]?.ToString() ?? "",
                    reader["Manufacturer"]?.ToString() ?? "",
                    reader["Model"]?.ToString() ?? "",
                    Convert.ToInt32(reader["Storage"]),
                    Convert.ToInt32(reader["ReleaseYear"]),
                    Convert.ToDouble(reader["Price"]),
                    Convert.ToInt32(reader["Stock"]),
                    reader["ImageFileName"]?.ToString() ?? ""
                ));
            }

            return phones;
        }


        // Add Phone
        public static bool InsertPhone(Phone phone)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = @"
                        INSERT INTO Phones
                        (PhoneID, Manufacturer, Model, Storage, ReleaseYear, Price, Stock, ImageFileName)
                        VALUES
                        (@PhoneID, @Manufacturer, @Model, @Storage, @ReleaseYear, @Price, @Stock, @ImageFileName)";

            using var cmd = new SqliteCommand(sql, connection);

            cmd.Parameters.AddWithValue("@PhoneID", phone.PhoneID);
            cmd.Parameters.AddWithValue("@Manufacturer", phone.Manufacturer);
            cmd.Parameters.AddWithValue("@Model", phone.Model);
            cmd.Parameters.AddWithValue("@Storage", phone.Storage);
            cmd.Parameters.AddWithValue("@ReleaseYear", phone.ReleaseYear);
            cmd.Parameters.AddWithValue("@Price", phone.Price);
            cmd.Parameters.AddWithValue("@Stock", phone.Stock);
            cmd.Parameters.AddWithValue("@ImageFileName", phone.ImageFileName ?? "");

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }


        // Remove Phone
        public static bool DeletePhone(string phoneId)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "DELETE FROM Phones WHERE PhoneID = @PhoneID";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@PhoneID", phoneId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // Update Phone Stock
        public static bool UpdatePhoneStock(string phoneId, int newStock)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "UPDATE Phones SET Stock = @Stock WHERE PhoneID = @PhoneID";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Stock", newStock);
            cmd.Parameters.AddWithValue("@PhoneID", phoneId);

            return cmd.ExecuteNonQuery() > 0;
        }


        // Update Phone Price
        public static bool UpdatePhonePrice(string phoneId, double newPrice)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "UPDATE Phones SET Price = @Price WHERE PhoneID = @PhoneID";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Price", newPrice);
            cmd.Parameters.AddWithValue("@PhoneID", phoneId);

            return cmd.ExecuteNonQuery() > 0;
        }


        // Insert Transaction
        public static bool InsertTransaction(Transaction t)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = @"
        INSERT INTO Transactions
        (OrderID, Date, Client, PhoneID, Phone, Quantity, Contract,
         Subtotal, DiscountPercent, DiscountAmount, TotalPaid, Payment, ProcessedBy)
        VALUES
        (@OrderID, @Date, @Client, @PhoneID, @Phone, @Quantity, @Contract,
         @Subtotal, @DiscountPercent, @DiscountAmount, @TotalPaid, @Payment, @ProcessedBy)";

            using var cmd = new SqliteCommand(sql, connection);

            cmd.Parameters.AddWithValue("@OrderID", t.OrderID);
            cmd.Parameters.AddWithValue("@Date", t.Date);
            cmd.Parameters.AddWithValue("@Client", t.Client);
            cmd.Parameters.AddWithValue("@PhoneID", t.PhoneID);
            cmd.Parameters.AddWithValue("@Phone", t.Phone);
            cmd.Parameters.AddWithValue("@Quantity", t.Quantity);
            cmd.Parameters.AddWithValue("@Contract", t.Contract);
            cmd.Parameters.AddWithValue("@Subtotal", t.Subtotal);
            cmd.Parameters.AddWithValue("@DiscountPercent", t.DiscountPercent);
            cmd.Parameters.AddWithValue("@DiscountAmount", t.DiscountAmount);
            cmd.Parameters.AddWithValue("@TotalPaid", t.TotalPaid);
            cmd.Parameters.AddWithValue("@Payment", t.Payment ?? "");
            cmd.Parameters.AddWithValue("@ProcessedBy", t.ProcessedBy ?? "");

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }


        // Load Transactions
        public static List<Transaction> LoadTransactions()
        {
            var history = new List<Transaction>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM Transactions ORDER BY Date DESC";

            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                history.Add(new Transaction
                {
                    OrderID = reader["OrderID"]?.ToString() ?? "",
                    Date = reader["Date"]?.ToString() ?? "",
                    Client = reader["Client"]?.ToString() ?? "",
                    PhoneID = reader["PhoneID"]?.ToString() ?? "",
                    Phone = reader["Phone"]?.ToString() ?? "",
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Contract = reader["Contract"]?.ToString() ?? "",
                    Subtotal = Convert.ToDouble(reader["Subtotal"]),
                    DiscountPercent = Convert.ToDouble(reader["DiscountPercent"]),
                    DiscountAmount = Convert.ToDouble(reader["DiscountAmount"]),
                    TotalPaid = Convert.ToDouble(reader["TotalPaid"]),
                    Payment = reader["Payment"]?.ToString() ?? "",
                    ProcessedBy = reader["ProcessedBy"]?.ToString() ?? ""
                });
            }

            return history;
        }

        


        // Insert Client
        public static bool InsertClient(Client client)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = @"
        INSERT INTO Clients
        (ClientType, Name, VAT, Email, ContactPhone, Address, Postcode, Town)
        VALUES
        (@ClientType, @Name, @VAT, @Email, @ContactPhone, @Address, @Postcode, @Town)";

            using var cmd = new SqliteCommand(sql, connection);

            cmd.Parameters.AddWithValue("@ClientType", client.ClientType);
            cmd.Parameters.AddWithValue("@Name", client.Name);
            cmd.Parameters.AddWithValue("@VAT", client.VAT);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@ContactPhone", client.ContactPhone);
            cmd.Parameters.AddWithValue("@Address", client.Address);
            cmd.Parameters.AddWithValue("@Postcode", client.Postcode);
            cmd.Parameters.AddWithValue("@Town", client.Town);

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }


        // Load Clients
        public static List<Client> LoadClients()
        {
            var clients = new List<Client>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM Clients ORDER BY Name ASC";

            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string clientType = reader["ClientType"]?.ToString() ?? "";
                string name = reader["Name"]?.ToString() ?? "";
                string vat = reader["VAT"]?.ToString() ?? "";
                string email = reader["Email"]?.ToString() ?? "";
                string contactPhone = reader["ContactPhone"]?.ToString() ?? "";
                string address = reader["Address"]?.ToString() ?? "";
                string postcode = reader["Postcode"]?.ToString() ?? "";
                string town = reader["Town"]?.ToString() ?? "";

                if (clientType.Equals("Company", StringComparison.OrdinalIgnoreCase))
                {
                    clients.Add(new Client(name, vat, email, contactPhone, address, postcode, town));
                }
                else
                {
                    clients.Add(new Client(name, email, contactPhone, address, postcode, town));
                }
            }

            return clients;
        }


        // Insert Inventory Log
        public static bool InsertInventoryLog(
            string timestamp,
            string performedBy,
            string action,
            string phone,
            string details)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = @"
                        INSERT INTO InventoryLogs
                        (Timestamp, PerformedBy, Action, Phone, Details)
                        VALUES
                        (@Timestamp, @PerformedBy, @Action, @Phone, @Details)";

            using var cmd = new SqliteCommand(sql, connection);

            cmd.Parameters.AddWithValue("@Timestamp", timestamp);
            cmd.Parameters.AddWithValue("@PerformedBy", performedBy);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@Details", details);

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        // Load Inventory Logs

        public static List<InventoryLog> LoadInventoryLogs()
        {
            var logs = new List<InventoryLog>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            string sql = "SELECT * FROM InventoryLogs ORDER BY Timestamp DESC";

            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                logs.Add(new InventoryLog
                {
                    Timestamp = reader["Timestamp"]?.ToString() ?? "",
                    PerformedBy = reader["PerformedBy"]?.ToString() ?? "",
                    Action = reader["Action"]?.ToString() ?? "",
                    Phone = reader["Phone"]?.ToString() ?? "",
                    Details = reader["Details"]?.ToString() ?? ""
                });
            }

            return logs;

        }
    }
}