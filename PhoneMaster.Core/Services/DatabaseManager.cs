using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace PhoneMaster.Core.Services
{
    public static class DatabaseManager
    {
        private static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string dataFolder = Path.Combine(basePath, "Data");
        private static readonly string dbPath = Path.Combine(dataFolder, "phonemaster.db");

        public static string ConnectionString => $"Data Source={dbPath}";

        public static void InitializeDatabase()
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

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
                    Stock INTEGER NOT NULL
                );
            ";

            using var command = new SqliteCommand(createPhonesTable, connection);
            command.ExecuteNonQuery();
        }
    }
}