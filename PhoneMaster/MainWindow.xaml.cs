using PhoneMaster.Core.Models;
using PhoneMaster.Core.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace PhoneMaster.GUI
{
    public partial class MainWindow : Window
    {
        private Inventory inventory = new Inventory();
        private PhoneMaster.Core.Models.Phone? selectedPhone;
        private int selectedQuantity;
        private PhoneMaster.Core.Services.Order? currentOrder;
        private List<PhoneMaster.Core.Services.Order> pendingOrders = new List<PhoneMaster.Core.Services.Order>();
        public MainWindow()
        {
            InitializeComponent();
            

        }

        private void LoadPhones()
        {
            PhonesGrid.ItemsSource = null;
            PhonesGrid.ItemsSource = inventory.GetPhones();
        }

        // HELPER METHOD 

        private void OrderInputChanged(object sender, RoutedEventArgs e)
        {
            UpdateOrderSummary();
        }

        private PhoneMaster.Core.Services.Order? BuildOrderFromForm()
        {
            if (CreateOrderPhonesGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a phone.");
                return null;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Enter a valid quantity.");
                return null;
            }

            Phone phone = (Phone)CreateOrderPhonesGrid.SelectedItem;

            if (quantity > phone.Stock)
            {
                MessageBox.Show("Not enough stock available.");
                return null;
            }

            // CLIENT TYPE
            if (ClientTypeBox.SelectedItem == null)
            {
                MessageBox.Show("Select client type.");
                return null;
            }

            string clientType =
                ((ComboBoxItem)ClientTypeBox.SelectedItem).Content.ToString()!;

            string name = ClientNameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string contactPhone = PhoneBox.Text.Trim();
            string address = AddressBox.Text.Trim();
            string postcode = PostcodeBox.Text.Trim();
            string town = TownBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(contactPhone) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(postcode) ||
                string.IsNullOrWhiteSpace(town))
            {
                MessageBox.Show("Please fill all client details.");
                return null;
            }

            Client client;

            if (clientType == "Customer")
            {
                client = new Client(name, email, contactPhone, address, postcode, town);
            }
            else
            {
                string vatNumber = VatNumberBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(vatNumber))
                {
                    MessageBox.Show("Enter VAT number for company client.");
                    return null;
                }

                client = new Client(name, vatNumber, email, contactPhone, address, postcode, town);
                UpdateOrderSummary();
            }

            // CONTRACT
            if (ContractTypeBox.SelectedItem == null)
            {
                MessageBox.Show("Select contract type.");
                return null;
            }

            string contractType =
                ((ComboBoxItem)ContractTypeBox.SelectedItem).Content.ToString()!;

            Contract contract;

            if (contractType == "SIM-Free")
            {
                contract = new SimFree(phone.Price);
            }
            else
            {
                if (PlanTypeBox.SelectedItem == null)
                {
                    MessageBox.Show("Select plan type.");
                    return null;
                }

                if (DurationBox.SelectedItem == null)
                {
                    MessageBox.Show("Select contract duration.");
                    return null;
                }

                string planTypeText =
                    ((ComboBoxItem)PlanTypeBox.SelectedItem).Content.ToString()!;

                PlanType planType = (planTypeText == "Standard")
                    ? PlanType.STANDARD
                    : PlanType.PREMIUM;

                int duration = int.Parse(DurationBox.SelectedItem.ToString()!);

                if (contractType == "Phone + SIM Package")
                {
                    contract = new PhoneSimPackage(phone.Price, duration, planType);
                }
                else if (contractType == "Hire Contract")
                {
                    if (clientType != "Company")
                    {
                        MessageBox.Show("Hire Contract is only available for company clients.");
                        return null;
                    }

                    contract = new HireContract(phone.Price, planType, duration);
                }
                else
                {
                    MessageBox.Show("Unsupported contract.");
                    return null;
                }
            }




            // CREATE ORDER
            var order = new PhoneMaster.Core.Services.Order(inventory);

            order.SetPhone(phone);
            order.SetQuantity(quantity);
            order.SetClient(client);
            order.SetContract(contract);

            order.CalculateTotal();

            return order;
        }


        private void ClientTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ClientTypeBox.SelectedItem == null)
                return;

            string clientType = ((ComboBoxItem)ClientTypeBox.SelectedItem).Content.ToString()!;

            if (clientType == "Company")
            {
                VatLabel.Visibility = Visibility.Visible;
                VatNumberBox.Visibility = Visibility.Visible;
            }
            else
            {
                VatLabel.Visibility = Visibility.Collapsed;
                VatNumberBox.Visibility = Visibility.Collapsed;
            }
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }

        private void Phones_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            PhonesPanel.Visibility = Visibility.Visible;
            LoadPhones();
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            CreateOrderPanel.Visibility = Visibility.Visible;

            CreateOrderPhonesGrid.ItemsSource = null;
            CreateOrderPhonesGrid.ItemsSource = inventory.GetPhones();
        }
        private void PreviewOrder_Click(object sender, RoutedEventArgs e)
        {
            var order = BuildOrderFromForm();
            if (order == null) return;

            currentOrder = order;

            var phone = order.GetPhone();
            var contract = order.GetContract();

            MessageBox.Show(
                $"======= ORDER SUMMARY =======\n" +
                $"Phone: {phone!.Manufacturer} {phone.Model} ({phone.Storage})\n" +
                $"Quantity: {order.GetQuantity()}\n" +
                $"Contract: {contract!.GetContractType()}\n" +
                $"TOTAL PRICE: £{order.GetTotalAfterDiscount():F2}"
            );
        }
        private void BackFromCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }

        private void NextCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (CreateOrderPhonesGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a phone.");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Enter a valid quantity.");
                return;
            }

            var phone = (PhoneMaster.Core.Models.Phone)CreateOrderPhonesGrid.SelectedItem;

            if (quantity > phone.Stock)
            {
                MessageBox.Show("Not enough stock available.");
                return;
            }

            selectedPhone = phone;
            selectedQuantity = quantity;

            MessageBox.Show(
                $"Selected: {phone.Manufacturer} {phone.Model} ({phone.Storage})\nQuantity: {quantity}"
            );
        }
        private void RefreshPendingOrdersGrid()
        {
            var displayList = pendingOrders.Select(order => new PendingOrderDisplay
            {
                OrderRef = order,
                ClientName = order.GetClient()?.Name ?? "",
                PhoneName = order.GetPhone() == null
                    ? ""
                    : $"{order.GetPhone()!.Manufacturer} {order.GetPhone()!.Model}",
                Quantity = order.GetQuantity(),
                ContractType = order.GetContract()?.GetContractType().ToString() ?? "",
                TotalPrice = order.GetTotalAfterDiscount()
            }).ToList();

            PendingOrdersGrid.ItemsSource = null;
            PendingOrdersGrid.ItemsSource = displayList;
        }

        private void ContractTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ContractTypeBox.SelectedItem == null)
                return;

            string contractType = ((System.Windows.Controls.ComboBoxItem)ContractTypeBox.SelectedItem).Content.ToString()!;

            DurationBox.Items.Clear();
            DurationLabel.Visibility = Visibility.Collapsed;
            DurationBox.Visibility = Visibility.Collapsed;

            if (contractType == "Phone + SIM Package")
            {
                DurationLabel.Text = "Contract Length (Months)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add("12");
                DurationBox.Items.Add("24");
            }
            else if (contractType == "Hire Contract")
            {
                DurationLabel.Text = "Hire Length (Years)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add("1");
                DurationBox.Items.Add("2");
            }
            UpdateOrderSummary();
        }
        private void SendOrder_Click(object sender, RoutedEventArgs e)
        {
            var order = BuildOrderFromForm();
            if (order == null) return;

            pendingOrders.Add(order);
            currentOrder = order;

            MessageBox.Show($"Order sent to staff. Pending orders: {pendingOrders.Count}");
        }

  
        
        private void UpdateOrderSummary()
        {
            try
            {
                if (CreateOrderPhonesGrid.SelectedItem == null)
                {
                    TotalPriceText.Text = "£0.00";
                    MonthlyCostText.Text = "£0.00";
                    return;
                }

                if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    TotalPriceText.Text = "£0.00";
                    MonthlyCostText.Text = "£0.00";
                    return;
                }

                Phone phone = (Phone)CreateOrderPhonesGrid.SelectedItem;

                if (ContractTypeBox.SelectedItem == null)
                {
                    TotalPriceText.Text = "£0.00";
                    MonthlyCostText.Text = "£0.00";
                    return;
                }

                string contractType = ((ComboBoxItem)ContractTypeBox.SelectedItem).Content.ToString()!;
                Contract contract;

                if (contractType == "SIM-Free")
                {
                    contract = new SimFree(phone.Price);
                }
                else
                {
                    if (PlanTypeBox.SelectedItem == null || DurationBox.SelectedItem == null)
                    {
                        TotalPriceText.Text = "£0.00";
                        MonthlyCostText.Text = "£0.00";
                        return;
                    }

                    string planTypeText = ((ComboBoxItem)PlanTypeBox.SelectedItem).Content.ToString()!;
                    PlanType planType = (planTypeText == "Standard")
                        ? PlanType.STANDARD
                        : PlanType.PREMIUM;

                    int duration = int.Parse(DurationBox.SelectedItem.ToString()!);

                    if (contractType == "Phone + SIM Package")
                    {
                        contract = new PhoneSimPackage(phone.Price, duration, planType);
                    }
                    else if (contractType == "Hire Contract")
                    {
                        contract = new HireContract(phone.Price, planType, duration);
                    }
                    else
                    {
                        TotalPriceText.Text = "£0.00";
                        MonthlyCostText.Text = "£0.00";
                        return;
                    }
                }

                double total = contract.CalculateTotal(quantity);
                double monthly = 0;

                if (contract is PhoneSimPackage p)
                {
                    monthly = total / p.Months;
                }
                else if (contract is HireContract h)
                {
                    monthly = total / (h.Years * 12);
                }

                TotalPriceText.Text = $"£{total:F2}";
                MonthlyCostText.Text = monthly > 0 ? $"£{monthly:F2}" : "N/A";
            }
            catch
            {
                TotalPriceText.Text = "£0.00";
                MonthlyCostText.Text = "£0.00";
            }
        }
        private void ProcessOrder_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            ProcessOrdersPanel.Visibility = Visibility.Visible;

            RefreshPendingOrdersGrid();

            MessageBox.Show($"Orders in list: {pendingOrders.Count}");
        }
        private void ProcessSelectedOrder_Click(object sender, RoutedEventArgs e)
        {
            var selected = PendingOrdersGrid.SelectedItem as PendingOrderDisplay;

            if (selected == null)
            {
                MessageBox.Show("Select an order first.");
                return;
            }

            var order = selected.OrderRef;

            order.UpdateInventory();
            order.RecordClient();
            order.AssignUkNumbersIfNeeded();
            order.RecordTransaction();
            order.GenerateReceipt();

            pendingOrders.Remove(order);

            RefreshPendingOrdersGrid();

            MessageBox.Show("Order processed successfully.");
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            var selected = PendingOrdersGrid.SelectedItem as PendingOrderDisplay;

            if (selected == null)
            {
                MessageBox.Show("Select an order first.");
                return;
            }

            pendingOrders.Remove(selected.OrderRef);

            RefreshPendingOrdersGrid();
        }

        private void BackFromProcessOrders_Click(object sender, RoutedEventArgs e)
        {
            ProcessOrdersPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }
        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Update Inventory screen");
        }

        private void Transactions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("View Transactions screen");
        }

        private void SearchPhone_Click(object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                LoadPhones();
                return;
            }

            PhonesGrid.ItemsSource = null;
            PhonesGrid.ItemsSource = inventory.SearchPhone(keyword);
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            PhonesPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }
    }
}