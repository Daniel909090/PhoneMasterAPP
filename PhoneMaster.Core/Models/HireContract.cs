using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class HireContract : Contract
    {
        public int Years { get; }
        public PlanType PlanType { get; }

        public HireContract(double unitPrice, PlanType planType, int years)
            : base("Hire Contract", unitPrice)
        {
            ContractType = ContractType.HIRE_CONTRACT;

            if (planType == null)
                throw new ArgumentException("PlanType cannot be null");

            PlanType = planType;
            Years = years;
        }

        public override double CalculateTotal(int quantity)
        {
            if (Years != 1 && Years != 2)
                throw new ArgumentException("Hire contract must be 1 or 2 years.");

            double yearlyFees = PlanType.YearlyHireFee * Years;
            double phonePercentage = (Years == 1) ? 0.25 : 0.50;
            double phoneCharge = UnitPrice * phonePercentage;
            double perPhoneTotal = yearlyFees + phoneCharge;

            return perPhoneTotal * quantity;
        }
    }
}