using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    /// <summary>
    /// Defines the categories of contracts available for phone acquisitions.
    /// </summary>
    public enum ContractType
    {
        /// <summary>
        /// Outright purchase of the handset with no network service included.
        /// </summary>
        SIM_FREE,

        /// <summary>
        /// Handset bundled with a monthly airtime and data plan.
        /// </summary>
        PHONE_SIM_PACKAGE,

        /// <summary>
        /// Long-term rental of the handset, for corporate clients.
        /// </summary>
        HIRE_CONTRACT
    }
}
