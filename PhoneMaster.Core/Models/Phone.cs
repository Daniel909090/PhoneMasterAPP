using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class Phone
    {
        public string PhoneID { get; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public int Storage { get; set; }
        public int ReleaseYear { get; }

        public string ImageFileName { get; set; }

        private double price;
        private int stock;

        public Phone(
            string phoneID,
            string manufacturer,
            string model,
            int storage,
            int releaseYear,
            double price,
            int stock,
            string imageFileName = "")
        {
            PhoneID = phoneID;
            Manufacturer = manufacturer;
            Model = model;
            Storage = storage;
            ReleaseYear = releaseYear;
            Price = price;
            Stock = stock;
            ImageFileName = imageFileName;
        }

        public double Price
        {
            get => price;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Price cannot be negative.");
                price = value;
            }
        }

        public int Stock
        {
            get => stock;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Stock cannot be negative.");
                stock = value;
            }
        }

        public override string ToString()
        {
            return $"{PhoneID} | {Manufacturer} | {Model} | {Storage}GB | Year {ReleaseYear} Price: £{Price} Stock: {Stock}";
        }
    }
}