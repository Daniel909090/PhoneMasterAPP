using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PhoneMaster.Core.Models;

namespace PhoneMaster.Core.Services
{
    public static class FileHandler
    {
        private const string PHONE_FILE = "phones.txt";
        private const string TRANSACTION_FILE = "transactions.txt";
        private const string RECEIPT_FOLDER = "receipts";
        private const string INVENTORY_LOG_FILE = "inventory_log.txt";
        private const string CLIENT_FILE = "clients.txt";
        private const string STAFF_FILE = "staff.txt";

        public static List<Phone> LoadPhones()
        {
            List<Phone> phones = new List<Phone>();

            try
            {
                using StreamReader sr = new StreamReader(PHONE_FILE);
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
                        double price = double.Parse(parts[5].Trim());
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
            try
            {
                using StreamWriter sw = new StreamWriter(PHONE_FILE);

                foreach (Phone p in phones)
                {
                    sw.WriteLine(
                        p.PhoneID + "|" +
                        p.Manufacturer + "|" +
                        p.Model + "|" +
                        p.Storage + "|" +
                        p.ReleaseYear + "|" +
                        p.Price + "|" +
                        p.Stock);
                }
            }
            catch
            {
            }
        }

        public static void SaveTransaction(string record)
        {
            try
            {
                using StreamWriter sw = new StreamWriter(TRANSACTION_FILE, true);
                sw.WriteLine(record);
            }
            catch
            {
            }
        }

        public static List<string> LoadTransactions()
        {
            List<string> transactions = new List<string>();

            if (!File.Exists(TRANSACTION_FILE))
            {
                return transactions; // file missing → empty list
            }

            try
            {
                using StreamReader sr = new StreamReader(TRANSACTION_FILE);
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
            try
            {
                if (!Directory.Exists(RECEIPT_FOLDER))
                {
                    Directory.CreateDirectory(RECEIPT_FOLDER);
                }

                string filePath = Path.Combine(RECEIPT_FOLDER, filename);
                File.WriteAllText(filePath, content);
            }
            catch
            {
            }
        }

        public static void SaveClient(Client client)
        {
            try
            {
                using StreamWriter sw = new StreamWriter(CLIENT_FILE, true);
                sw.WriteLine(client.ToRecord());
            }
            catch
            {
            }
        }

        public static List<string> LoadClients()
        {
            List<string> clients = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(CLIENT_FILE);
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
            List<string> staff = new List<string>();

            try
            {
                using StreamReader sr = new StreamReader(STAFF_FILE);
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
            List<string> logs = new List<string>();

            try
            {
                if (!File.Exists(INVENTORY_LOG_FILE))
                    return logs;

                using StreamReader sr = new StreamReader(INVENTORY_LOG_FILE);
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

        public static void SaveInventoryLog(string action,
                                            string phoneID,
                                            string phoneModel,
                                            string details,
                                            string performedBy)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string logEntry = timestamp +
                              " | " + performedBy +  
                              " | " + action +
                              " | " + phoneID + " " + phoneModel +
                              " | " + details;

            try
            {
                using StreamWriter sw = new StreamWriter(INVENTORY_LOG_FILE, true);
                sw.WriteLine(logEntry);
            }
            catch
            {
            }
        }
    }
}