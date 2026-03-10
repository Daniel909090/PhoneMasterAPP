using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class PlanType
    {
        public static readonly PlanType STANDARD = new PlanType(10.0, 120.0);
        public static readonly PlanType PREMIUM = new PlanType(20.0, 240.0);

        public double MonthlyFee { get; }
        public double YearlyHireFee { get; }

        private PlanType(double monthlyFee, double yearlyHireFee)
        {
            MonthlyFee = monthlyFee;
            YearlyHireFee = yearlyHireFee;
        }
    }
}
