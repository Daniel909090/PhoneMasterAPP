using System;
using System.Collections.Generic;
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

        public bool AddPhone(Phone phone)
        {
            if (phone.Stock < 0 || phone.Stock > 100)
                return false;

            phones.Add(phone);
            FileHandler.SavePhones(phones);
            return true;
        }

        public bool RemovePhone(string phoneID)
        {
            bool removed = phones.RemoveAll(p =>
                p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                FileHandler.SavePhones(phones);
            }

            return removed;
        }

        public bool UpdateStock(string phoneID, int newStock)
        {
            if (newStock < 0 || newStock > 100)
                return false;

            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    p.Stock = newStock;
                    FileHandler.SavePhones(phones);
                    return true;
                }
            }

            return false;
        }

        public bool ChangePrice(string phoneID, double newPrice)
        {
            foreach (Phone p in phones)
            {
                if (p.PhoneID.Equals(phoneID, StringComparison.OrdinalIgnoreCase))
                {
                    p.Price = newPrice;
                    FileHandler.SavePhones(phones);
                    return true;
                }
            }

            return false;
        }

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
    }
}