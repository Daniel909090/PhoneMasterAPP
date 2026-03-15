using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class PlanType
    {
        public static readonly PlanType STANDARD = new PlanType("Standard", 10, 120);
        public static readonly PlanType PREMIUM = new PlanType("Premium", 20, 240);

        public string Name { get; }
        public double MonthlyFee { get; }
        public double YearlyHireFee { get; }

        private PlanType(string name, double monthlyFee, double yearlyHireFee)
        {
            Name = name;
            MonthlyFee = monthlyFee;
            YearlyHireFee = yearlyHireFee;
        }

        public override string ToString() => Name;
    }
}
