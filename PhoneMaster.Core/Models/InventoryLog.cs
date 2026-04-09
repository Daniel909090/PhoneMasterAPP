using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class InventoryLog
    {
        public string Timestamp { get; set; } = "";
        public string PerformedBy { get; set; } = "";
        public string Action { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Details { get; set; } = "";
    }
}