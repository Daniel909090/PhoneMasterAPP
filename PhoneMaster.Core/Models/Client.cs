using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneMaster.Core.Models
{
    public class Client
    {
        public string Name { get; }
        public string Email { get; }
        public string ContactPhone { get; }
        public string Address { get; }
        public string Postcode { get; }
        public string Town { get; }
        public bool IsCompany { get; }
        public string? VatNumber { get; }

        // Constructor for normal customers
        public Client(string name, string email, string contactPhone,
                      string address, string postcode, string town)
        {
            Name = name;
            Email = email;
            ContactPhone = contactPhone;
            Address = address;
            Postcode = postcode;
            Town = town;
            IsCompany = false;
            VatNumber = null;
        }

        // Constructor for company clients
        public Client(string name, string vatNumber, string email, string contactPhone,
                      string address, string postcode, string town)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                throw new ArgumentException("VAT number is required for company clients");

            Name = name;
            VatNumber = vatNumber;
            Email = email;
            ContactPhone = contactPhone;
            Address = address;
            Postcode = postcode;
            Town = town;
            IsCompany = true;
        }

        public string ToRecord()
        {
            if (IsCompany)
            {
                return "COMPANY|" +
                       Name + "|" +
                       VatNumber + "|" +
                       Email + "|" +
                       ContactPhone + "|" +
                       Address + "|" +
                       Postcode + "|" +
                       Town;
            }

            return "CUSTOMER|" +
                   Name + "|" +
                   Email + "|" +
                   ContactPhone + "|" +
                   Address + "|" +
                   Postcode + "|" +
                   Town;
        }

        public override string ToString()
        {
            if (IsCompany)
            {
                return $"{Name} (Company, VAT: {VatNumber}), {Address}, {Town} {Postcode}";
            }

            return $"{Name} (Customer), {Address}, {Town} {Postcode}";
        }
    }
}