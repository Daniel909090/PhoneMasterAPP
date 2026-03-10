using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PhoneMaster.Core.Services;

namespace PhoneMaster.Core.Models
{
    public class Staff
    {
        public string Username { get; }
        private string Password { get; }
        private string Role { get; }

        public Staff(string username, string password, string role)
        {
            Username = username == null ? "" : username.Trim();
            Password = password == null ? "" : password.Trim();
            Role = role == null ? "" : role.Trim().ToUpper();
        }

        // ================= ACCESS CONTROL =================

        public bool CanProcessOrders()
        {
            return Role == "STAFF" || Role == "MANAGER";
        }

        public bool CanApproveDiscount()
        {
            return Role == "MANAGER";
        }

        public bool CanUpdateInventory()
        {
            return Role == "CENTRAL";
        }

        public bool CanViewTransactions()
        {
            return Role == "STAFF" || Role == "CENTRAL" || Role == "MANAGER";
        }

        // ================= AUTHENTICATION =================

        public bool Authenticate(string user, string pass)
        {
            if (user == null || pass == null) return false;

            return Username.Equals(user.Trim(), StringComparison.OrdinalIgnoreCase)
                   && Password == pass.Trim();
        }

        public static Staff? LoginFromFile(string username, string password)
        {
            var lines = FileHandler.LoadStaff();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('|');
                if (parts.Length < 3) continue;

                string fileUser = parts[0].Trim();
                string filePass = parts[1].Trim();
                string fileRole = parts[2].Trim().ToUpper();

                if (fileRole != "STAFF" && fileRole != "CENTRAL" && fileRole != "MANAGER")
                    continue;

                var staff = new Staff(fileUser, filePass, fileRole);

                if (staff.Authenticate(username, password))
                    return staff;
            }

            return null;
        }
    }
}