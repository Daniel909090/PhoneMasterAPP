using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class SimFree : Contract
    {
        public SimFree(double basePrice)
            : base("SIM Free Purchase", basePrice)
        {
            ContractType = ContractType.SIM_FREE;
        }

        public override double CalculateTotal(int quantity)
        {
            return UnitPrice * quantity;
        }
    }
}
