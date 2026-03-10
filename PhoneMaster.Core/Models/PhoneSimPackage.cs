using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class PhoneSimPackage : Contract
    {
        public int Months { get; }
        public PlanType PlanType { get; }

        public PhoneSimPackage(double basePrice, int months, PlanType planType)
            : base("Phone + SIM Package", basePrice)
        {
            ContractType = ContractType.PHONE_SIM_PACKAGE;

            if (planType == null)
                throw new ArgumentException("PlanType cannot be null");

            if (months != 12 && months != 24)
                throw new ArgumentException("Months must be 12 or 24");

            Months = months;
            PlanType = planType;
        }

        public override double CalculateTotal(int quantity)
        {
            double phoneCost = UnitPrice * quantity;
            double planCost = PlanType.MonthlyFee * Months * quantity;

            return phoneCost + planCost;
        }
    }
}