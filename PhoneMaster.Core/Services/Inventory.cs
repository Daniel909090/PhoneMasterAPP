using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PhoneMaster.Core.Models;

namespace PhoneMaster.Core.Services
{
    public class Inventory
    {
        private List<Phone> phones;

        public Inventory()
        {
            phones = new List<Phone>();
        }

        // Constructor for testing only
        // Used for unit testing (inject mock data)
        public Inventory(List<Phone> phones)
        {
            this.phones = phones ?? new List<Phone>();
        }

        public void LoadPhones()
        {
            phones = DatabaseManager.LoadPhones();
        }


        public List<Phone> GetPhones()
        {
            return phones;
        }

        
        public List<Phone> SearchPhone(string keyword)
        {
            List<Phone> results = new List<Phone>();
            string search = keyword.ToLower();

            foreach (Phone p in phones)
            {
                if (p.Manufacturer.ToLower().Contains(search) ||
                    p.Model.ToLower().Contains(search))
                {
                    results.Add(p);
                }
            }

            return results;
        }

        public Phone? SearchPhoneID(string phoneID)
        {
            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }

            return null;
        }


        public string GenerateNextPhoneID()
        {
            int max = 0;

            foreach (Phone p in phones)
            {
                if (p.PhoneID != null &&
                    p.PhoneID.StartsWith("P") &&
                    int.TryParse(p.PhoneID.Substring(1), out int num))
                {
                    if (num > max)
                        max = num;
                }
            }

            return $"P{max + 1:D3}";
        }


        //Add Phone
        public bool AddPhone(Phone phone, string performedBy)
        {
            if (phone == null)
                return false;

            if (phone.Price < 0)
                return false;

            if (phone.Stock < 0 || phone.Stock > 100)
                return false;

            bool added = DatabaseManager.InsertPhone(phone);

            if (!added)
                return false;

            DatabaseManager.InsertInventoryLog(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                performedBy,
                "ADD PHONE",
                $"{phone.PhoneID} {phone.Manufacturer} {phone.Model} ({phone.Storage}GB)",
                $"Added with price £{phone.Price:F2}, stock {phone.Stock}, release year {phone.ReleaseYear}"
            );

            LoadPhones();
            return true;
        }

        //Remove Phone
        public bool RemovePhone(string phoneID, string performedBy)
        {
            Phone? phoneToRemove = SearchPhoneID(phoneID);

            if (phoneToRemove == null)
                return false;

            bool removed = DatabaseManager.DeletePhone(phoneID);

            if (!removed)
                return false;

            DatabaseManager.InsertInventoryLog(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                performedBy,
                "REMOVE PHONE",
                $"{phoneToRemove.PhoneID} {phoneToRemove.Manufacturer} {phoneToRemove.Model}",
                "Phone removed from inventory"
            );

            LoadPhones();
            return true;
        }

        //Update Stock -  Manually update
        public bool UpdateStock(string phoneID, int newStock, string performedBy)
        {
            if (newStock < 0 || newStock > 100)
                return false;

            Phone? phoneToUpdate = SearchPhoneID(phoneID);

            if (phoneToUpdate == null)
                return false;

            int oldStock = phoneToUpdate.Stock;

            bool updated = DatabaseManager.UpdatePhoneStock(phoneID, newStock);

            if (!updated)
                return false;

            DatabaseManager.InsertInventoryLog(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                performedBy,
                "UPDATE STOCK",
                $"{phoneToUpdate.PhoneID} {phoneToUpdate.Manufacturer} {phoneToUpdate.Model} ({phoneToUpdate.Storage}GB)",
                $"Stock changed from {oldStock} to {newStock}"
            );

            LoadPhones();
            return true;
        }

        //Change Price
        public bool ChangePrice(string phoneID, double newPrice, string performedBy)
        {
            if (newPrice <= 0)
                return false;

            Phone? phoneToUpdate = SearchPhoneID(phoneID);

            if (phoneToUpdate == null)
                return false;

            double oldPrice = phoneToUpdate.Price;

            bool updated = DatabaseManager.UpdatePhonePrice(phoneID, newPrice);

            if (!updated)
                return false;

            DatabaseManager.InsertInventoryLog(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                performedBy,
                "CHANGE PRICE",
                $"{phoneToUpdate.PhoneID} {phoneToUpdate.Manufacturer} {phoneToUpdate.Model} ({phoneToUpdate.Storage}GB)",
                $"Price changed from £{oldPrice:F2} to £{newPrice:F2}"
            );

            LoadPhones();
            return true;
        }


        //Automated method that reduces the stock after order is successfully processed
        public bool ReduceStock(string phoneID, int quantity)
        {
            if (quantity <= 0)
                return false;

            Phone? phoneToUpdate = SearchPhoneID(phoneID);

            if (phoneToUpdate == null)
                return false;

            if (phoneToUpdate.Stock < quantity)
                return false;

            int newStock = phoneToUpdate.Stock - quantity;

            bool updated = DatabaseManager.UpdatePhoneStock(phoneID, newStock);

            if (!updated)
                return false;

            LoadPhones();
            return true;
        }


        // Method to calculate total revenue, total discount applied, and total phones sold
        public void GetShopBalanceData(out double revenue, out double totalDiscountApplied, out int totalPhonesSold)
        {
            revenue = 0;
            totalDiscountApplied = 0;
            totalPhonesSold = 0;

            List<Transaction> transactions = DatabaseManager.LoadTransactions();

            foreach (Transaction t in transactions)
            {
                revenue += t.TotalPaid;
                totalDiscountApplied += t.DiscountAmount;
                totalPhonesSold += t.Quantity;
            }
        }


        public double CalculateStockValue()
        {
            double total = 0;

            foreach (Phone p in phones)
            {
                total += p.Price * p.Stock;
            }

            return total;
        }


        public int GetTotalStockUnits()
        {
            int total = 0;

            foreach (Phone p in phones)
            {
                total += p.Stock;
            }

            return total;
        }

        

    }
}