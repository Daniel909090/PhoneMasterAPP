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
            phones = FileHandler.LoadPhones();
        }

        // Constructor for tests
        public Inventory(List<Phone> phones)
        {
            this.phones = phones ?? new List<Phone>();
        }

        public List<Phone> GetPhones()
        {
            return phones;
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

        public string GenerateNextPhoneID()
        {
            int max = 0;

            foreach (Phone p in phones)
            {
                try
                {
                    int num = int.Parse(p.PhoneID.Substring(1));
                    if (num > max) max = num;
                }
                catch
                {
                }
            }

            int next = max + 1;
            return $"P{next:D3}";
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

        //Add Phone
        public bool AddPhone(Phone phone, string performedBy)
        {
            if (phone.Stock < 0 || phone.Stock > 100)
                return false;

            phones.Add(phone);
            FileHandler.SavePhones(phones);

            //  LOGGING Mehodd added
            FileHandler.SaveInventoryLog(
                "ADD PHONE",
                phone.PhoneID,
                $"{phone.Manufacturer} {phone.Model}",
                $"Added with price £{phone.Price:F2}, stock {phone.Stock}, storage {phone.Storage}GB",
                performedBy
            );

            return true;
        }

        //Remove Phone
        public bool RemovePhone(string phoneID, string performedBy)
        {
            Phone? phoneToRemove = SearchPhoneID(phoneID);

            if (phoneToRemove == null)
                return false;

            bool removed = phones.RemoveAll(p =>
                p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                FileHandler.SavePhones(phones);

                FileHandler.SaveInventoryLog(
                    "REMOVE PHONE",
                    phoneToRemove.PhoneID,
                    $"{phoneToRemove.Manufacturer} {phoneToRemove.Model}",
                    "Phone removed from inventory",
                    performedBy
                );
            }

            return removed;
        }

        //Update Stock
        public bool UpdateStock(string phoneID, int newStock, string performedBy)
        {
            if (newStock < 0 || newStock > 100)
                return false;

            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    int oldStock = p.Stock;

                    p.Stock = newStock;
                    FileHandler.SavePhones(phones);

                    FileHandler.SaveInventoryLog(
                        "UPDATE STOCK",
                        p.PhoneID,
                        $"{p.Manufacturer} {p.Model}",
                        $"Stock changed from {oldStock} to {newStock}",
                        performedBy
                    );

                    return true;
                }
            }

            return false;
        }

        //Change Price
        public bool ChangePrice(string phoneID, double newPrice, string performedBy)
        {
            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    double oldPrice = p.Price;

                    p.Price = newPrice;
                    FileHandler.SavePhones(phones);

                    FileHandler.SaveInventoryLog(
                        "CHANGE PRICE",
                        p.PhoneID,
                        $"{p.Manufacturer} {p.Model}",
                        $"Price changed from £{oldPrice:F2} to £{newPrice:F2}",
                        performedBy
                    );

                    return true;
                }
            }

            return false;
        }

        //Automated method that reduces the stock after order is successfully processed
        public bool ReduceStock(string phoneID, int quantity)
        {
            if (quantity <= 0) return false;

            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    if (p.Stock < quantity) return false;

                    p.Stock = p.Stock - quantity;
                    FileHandler.SavePhones(phones);
                    return true;
                }
            }

            return false;
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