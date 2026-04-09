using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class Transaction
    {
        public string OrderID { get; set; } = "";
        public string Date { get; set; } = "";
        public string Client { get; set; } = "";
        public string PhoneID { get; set; } = "";
        public string Phone { get; set; } = "";
        public int Quantity { get; set; }
        public string Contract { get; set; } = "";
        public double Subtotal { get; set; }
        public double DiscountPercent { get; set; }
        public double DiscountAmount { get; set; }
        public double TotalPaid { get; set; }
        public string Payment { get; set; } = "";
        public string ProcessedBy { get; set; } = "";
    }

}
