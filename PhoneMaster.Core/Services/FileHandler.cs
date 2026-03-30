using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PhoneMaster.Core.Models;

namespace PhoneMaster.Core.Services
{
    public static class FileHandler
    {
        private static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string dataPath = Path.Combine(basePath, "Data");

        private static readonly string phoneFilePath = Path.Combine(dataPath, "phones.txt");
        private static readonly string transactionFilePath = Path.Combine(dataPath, "transactions.txt");
        private static readonly string inventoryLogFilePath = Path.Combine(dataPath, "inventory_log.txt");
        private static readonly string clientFilePath = Path.Combine(dataPath, "clients.txt");
        private static readonly string staffFilePath = Path.Combine(dataPath, "staff.txt");
        private static readonly string receiptFolderPath = Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PhoneMaster","Receipts" );

        private static void EnsureFilesExist()
        {
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(receiptFolderPath);

            if (!File.Exists(phoneFilePath)) File.WriteAllText(phoneFilePath, "");
            if (!File.Exists(clientFilePath)) File.WriteAllText(clientFilePath, "");
            if (!File.Exists(transactionFilePath)) File.WriteAllText(transactionFilePath, "");
            if (!File.Exists(inventoryLogFilePath)) File.WriteAllText(inventoryLogFilePath, "");
            if (!File.Exists(staffFilePath)) File.WriteAllText(staffFilePath, "");
        }

        public static List<Phone> LoadPhones()
        {
            EnsureFilesExist();

            List<Phone> phones = new List<Phone>();

            try
            {
                using StreamReader sr = new StreamReader(phoneFilePath);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    string[] parts = trimmed.Split('|');
                    if (parts.Length != 7)
                        continue;

                    try
                    {
                        string phoneID = parts[0].Trim();
                        string manufacturer = parts[1].Trim();
                        string model = parts[2].Trim();
                        int storage = int.Parse(parts[3].Trim());
                        int releaseYear = int.Parse(parts[4].Trim());
                        double price = double.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
                        int stock = int.Parse(parts[6].Trim());

                        phones.Add(new Phone(phoneID, manufacturer, model, storage, releaseYear, price, stock));
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return phones;
        }

        public static void SavePhones(List<Phone> phones)
        {
            EnsureFilesExist();

            try
            {
                using StreamWriter sw = new StreamWriter(phoneFilePath, false);

                foreach (Phone p in phones)
                {
                    sw.WriteLine(
                        p.PhoneID + "|" +
                        p.Manufacturer + "|" +
                        p.Model + "|" +
                        p.Storage + "|" +
                        p.ReleaseYear + "|" +
                        p.Price.ToString(CultureInfo.InvariantCulture) + "|" +
                        p.Stock);
                }
            }
            catch
            {
            }
        }

        public static void SaveTransaction(string record)
        {
            EnsureFilesExist();

            try
            {
                using StreamWriter sw = new StreamWriter(transactionFilePath, true);
                sw.WriteLine(record);
            }
            catch
            {
            }
        }

        public static List<string> LoadTransactions()
        {
            EnsureFilesExist();

            List<string> transactions = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(transactionFilePath);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        transactions.Add(line);
                    }
                }
            }
            catch
            {
            }

            return transactions;
        }

        public static void WriteReceipt(string filename, string content)
        {
            EnsureFilesExist();

            try
            {
                string filePath = Path.Combine(receiptFolderPath, filename);
                File.WriteAllText(filePath, content);
            }
            catch
            {
            }
        }

        public static void SaveClient(Client client)
        {
            EnsureFilesExist();

            try
            {
                using StreamWriter sw = new StreamWriter(clientFilePath, true);
                sw.WriteLine(client.ToRecord());
            }
            catch
            {
            }
        }

        public static List<string> LoadClients()
        {
            EnsureFilesExist();

            List<string> clients = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(clientFilePath);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        clients.Add(line);
                    }
                }
            }
            catch
            {
            }

            return clients;
        }

        public static List<string> LoadStaff()
        {
            EnsureFilesExist();

            List<string> staff = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(staffFilePath);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        staff.Add(line);
                    }
                }
            }
            catch
            {
            }

            return staff;
        }

        public static List<string> LoadInventoryLogs()
        {
            EnsureFilesExist();

            List<string> logs = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(inventoryLogFilePath);
                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        logs.Add(line);
                    }
                }
            }
            catch
            {
            }

            return logs;
        }

        public static void SaveInventoryLog(
            string action,
            string phoneID,
            string phoneModel,
            string details,
            string performedBy)
        {
            EnsureFilesExist();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string logEntry =
                timestamp + "|" +
                performedBy + "|" +
                action + "|" +
                phoneID + " " + phoneModel + "|" +
                details;

            try
            {
                using StreamWriter sw = new StreamWriter(inventoryLogFilePath, true);
                sw.WriteLine(logEntry);
            }
            catch
            {
            }
        }
    }
}