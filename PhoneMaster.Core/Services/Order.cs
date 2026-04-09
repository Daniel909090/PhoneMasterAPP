using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using PhoneMaster.Core.Models;

namespace PhoneMaster.Core.Services
{
    public class Order
    {
        private readonly string orderID;
        private readonly DateTime date;
        private Client? client;
        private Phone? phone;
        private int quantity;
        private Contract? contract;
        private double baseTotal;
        private double discount;
        private double discountAmount;
        private double totalAfterDiscount;
        private string? paymentMethod;
        private string? accountNumber;
        private string? sortCode;
        private const double MAX_DISCOUNT = 20.0;
        private bool monthlyPayment;
        private double monthlyAmount;

        private readonly List<string> assignedUkNumbers = new();
        private string? processedBy;

        private readonly Inventory inventory;

        public Order(Inventory inventory)
        {
            this.inventory = inventory;
            orderID = GenerateUniqueID();
            date = DateTime.Now;
            discount = 0;
        }

        public Phone? GetPhone()
        {
            return phone;
        }

        public void SetPhone(Phone phone)
        {
            this.phone = phone;
        }

        public Client? GetClient()
        {
            return client;
        }

        public void SetClient(Client client)
        {
            this.client = client;
        }

        public Contract? GetContract()
        {
            return contract;
        }

        public void SetContract(Contract contract)
        {
            this.contract = contract;
        }

        public int GetQuantity()
        {
            return quantity;
        }

        public void SetQuantity(int quantity)
        {
            this.quantity = quantity;
        }

        public string? GetPaymentMethod()
        {
            return paymentMethod;
        }

        public void SetPaymentMethod(string method)
        {
            paymentMethod = method;
        }

        public void SetMonthlyPayment(bool monthlyPayment)
        {
            this.monthlyPayment = monthlyPayment;
        }

        public void SetMonthlyAmount(double monthlyAmount)
        {
            this.monthlyAmount = monthlyAmount;
        }

        public void SetCardDetails(string accountNumber, string sortCode)
        {
            this.accountNumber = accountNumber;
            this.sortCode = sortCode;
        }

        public void SetProcessedBy(string processedBy)
        {
            this.processedBy = processedBy;
        }

        public double GetDiscount()
        {
            return discount;
        }

        public double GetBaseTotal()
        {
            return baseTotal;
        }

        public double GetDiscountAmount()
        {
            return discountAmount;
        }

        public double GetTotalAfterDiscount()
        {
            return totalAfterDiscount;
        }

        public string? GetAccountNumber()
        {
            return accountNumber;
        }

        public string? GetSortCode()
        {
            return sortCode;
        }

        private string GetContractTypeName(ContractType contractType)
        {
            if (contractType == ContractType.SIM_FREE) return "SIM_FREE";
            if (contractType == ContractType.PHONE_SIM_PACKAGE) return "PHONE_SIM_PACKAGE";
            if (contractType == ContractType.HIRE_CONTRACT) return "HIRE_CONTRACT";
            return "";
        }

        private string GetPlanTypeName(PlanType planType)
        {
            if (planType == PlanType.STANDARD) return "STANDARD";
            if (planType == PlanType.PREMIUM) return "PREMIUM";
            return "";
        }


        public bool ApplyDiscount(double discountPercent)
        {
            if (discountPercent < 0 || discountPercent > MAX_DISCOUNT)
                return false;

            discount = discountPercent;
            return true;
        }


        public double CalculateTotal()
        {
            if (contract == null)
                throw new InvalidOperationException("Contract is not set.");

            baseTotal = contract.CalculateTotal(quantity);
            discountAmount = baseTotal * (discount / 100);
            totalAfterDiscount = baseTotal - discountAmount;

            return totalAfterDiscount;
        }


        public double CalculateMonthlyPayment()
        {
            double total = GetTotalAfterDiscount();

            if (contract is PhoneSimPackage p)
                return total / p.Months;

            if (contract is HireContract h)
                return total / (h.Years * 12);

            return 0;
        }


        public void UpdateInventory()
        {
            if (phone == null)
                throw new InvalidOperationException("Phone is not set.");

            bool success = inventory.ReduceStock(phone.PhoneID, quantity);

            if (!success)
                return;
        }


        public void RecordTransaction()
        {
            if (client == null || phone == null || contract == null)
                throw new InvalidOperationException("Order is incomplete.");

            var transaction = new Transaction
            {
                OrderID = orderID,
                Date = date.ToString("dd/MM/yyyy HH:mm"),
                Client = client.Name,
                PhoneID = phone.PhoneID,
                Phone = phone.Manufacturer + " " + phone.Model,
                Quantity = quantity,
                Contract = GetContractTypeName(contract.GetContractType()),
                Subtotal = baseTotal,
                DiscountPercent = discount,
                DiscountAmount = discountAmount,
                TotalPaid = totalAfterDiscount,
                Payment = paymentMethod ?? "",
                ProcessedBy = processedBy ?? ""
            };

            bool saved = DatabaseManager.InsertTransaction(transaction);

            if (!saved)
                throw new InvalidOperationException("Transaction could not be saved.");
        }


        public void RecordClient()
        {
            if (client == null)
                throw new InvalidOperationException("Client is not set.");

            bool saved = DatabaseManager.InsertClient(client);

            if (!saved)
                throw new InvalidOperationException("Client could not be saved.");
        }


        private string MaskAccountNumber(string? acc)
        {
            if (string.IsNullOrWhiteSpace(acc) || acc.Length != 8)
                return "********";

            return "**** " + acc.Substring(4);
        }


        public bool RequiresUkNumbers()
        {
            return contract is PhoneSimPackage || contract is HireContract;
        }


        public void AssignUkNumbersIfNeeded()
        {
            assignedUkNumbers.Clear();

            if (!RequiresUkNumbers())
                return;

            int qty = GetQuantity();
            if (qty <= 0)
                return;

            HashSet<string> unique = new();
            Random rnd = new();

            while (assignedUkNumbers.Count < qty)
            {
                int part1 = rnd.Next(0, 100000);
                int part2 = rnd.Next(0, 10000);
                string num = "07" + part1.ToString("D5") + part2.ToString("D4");

                if (unique.Add(num))
                {
                    assignedUkNumbers.Add(num);
                }
            }
        }


        private string GenerateUniqueID()
        {
            return DateTime.Now.ToString("yyMMddHHmmss");
        }


        public void GenerateReceipt()
        {
            if (client == null || phone == null || contract == null)
                throw new InvalidOperationException("Order is incomplete.");

            string filename = $"receipt_{orderID} {client.Name}.txt";
            string dateStr = date.ToString("dd/MM/yyyy HH:mm");

            var sb = new System.Text.StringBuilder();

            sb.Append("========== RECEIPT ==========\n")
              .Append("Order ID: ").Append(orderID).Append("\n")
              .Append("Date: ").Append(dateStr).Append("\n")
              .Append("Processed by: ").Append(processedBy).Append("\n\n")
              .Append("===========================\n")
              .Append("Client: ").Append(client.Name).Append("\n")
              .Append("Phone ID: ").Append(phone.PhoneID).Append("\n")
              .Append("Manufacturer: ").Append(phone.Manufacturer).Append("\n")
              .Append("Model: ").Append(phone.Model).Append("\n")
              .Append("Unit Price: £").Append(phone.Price).Append("\n")
              .Append("Storage: ").Append(phone.Storage).Append("\n")
              .Append("Quantity: ").Append(quantity).Append("\n")
              .Append("Contract: ").Append(GetContractTypeName(contract.GetContractType())).Append("\n");

            if (contract is PhoneSimPackage p)
            {
                sb.Append("Contract Length: ").Append(p.Months).Append(" months\n")
                  .Append("Plan Type: ").Append(GetPlanTypeName(p.PlanType)).Append("\n");
            }
            else if (contract is HireContract h)
            {
                sb.Append("Hire Length: ").Append(h.Years).Append(" years\n")
                  .Append("Plan Type: ").Append(GetPlanTypeName(h.PlanType)).Append("\n");
            }

            sb.Append("SUBTOTAL: £").Append(GetBaseTotal().ToString("F2")).Append("\n")
              .Append("Discount(").Append(discount).Append("%):-£").Append(GetDiscountAmount().ToString("F2")).Append("\n")
              .Append("-----------------------\n")
              .Append("FINAL PRICE: £").Append(GetTotalAfterDiscount().ToString("F2")).Append("\n");

            double vat = GetTotalAfterDiscount() * 20 / 100;

            sb.Append("VAT (20%): £").Append(vat.ToString("F2")).Append("\n")
              .Append("-----------------------\n")
              .Append("Payment Method: ").Append(paymentMethod).Append("\n");

            if (monthlyPayment)
            {
                sb.Append("Payment Plan: MONTHLY\n")
                  .Append("Monthly Amount: £").Append(monthlyAmount.ToString("F2")).Append("\n")
                  .Append("Amount Paid Today: £").Append(monthlyAmount.ToString("F2")).Append("\n");
            }
            else
            {
                sb.Append("Payment Plan: FULL\n")
                  .Append("Amount Paid Today: £").Append(GetTotalAfterDiscount().ToString("F2")).Append("\n")
                  .Append("===========================\n");
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod) &&
                paymentMethod.Equals("CARD", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("Account Number: ").Append(MaskAccountNumber(accountNumber)).Append("\n")
                  .Append("Sort Code: ").Append(sortCode).Append("\n");
            }

            if (assignedUkNumbers.Count > 0)
            {
                sb.Append("\nAssigned UK Phone Numbers:\n");
                for (int i = 0; i < assignedUkNumbers.Count; i++)
                {
                    sb.Append("  ").Append(i + 1).Append(") ").Append(assignedUkNumbers[i]).Append("\n");
                }
            }

            sb.Append("============================");

            FileHandler.WriteReceipt(filename, sb.ToString());
        }
    }
}