using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public abstract class Contract
    {
        protected ContractType ContractType;
        protected string Name;
        protected double UnitPrice; // price of the phone when contract is created

        protected Contract(string name, double unitPrice)
        {
            Name = name;
            UnitPrice = unitPrice;
        }

        public ContractType GetContractType()
        {
            return ContractType;
        }

        public string GetName()
        {
            return Name;
        }

        // Calculates the total cost for each contract
        public abstract double CalculateTotal(int quantity);
    }
}