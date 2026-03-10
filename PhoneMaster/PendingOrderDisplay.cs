using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.GUI
{
    public class PendingOrderDisplay
    {
        public PhoneMaster.Core.Services.Order OrderRef { get; set; }

        public string ClientName { get; set; }
        public string PhoneName { get; set; }
        public int Quantity { get; set; }
        public string ContractType { get; set; }
        public double TotalPrice { get; set; }
    }
}