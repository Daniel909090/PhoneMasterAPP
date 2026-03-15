using PhoneMaster.Core.Models;
using PhoneMaster.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhoneMaster.GUI
{
    public partial class MainWindow : Window
    {
        private Inventory inventory = new Inventory();
        
        private PhoneMaster.Core.Services.Order? currentOrder;
        private List<PhoneMaster.Core.Services.Order> pendingOrders = new List<PhoneMaster.Core.Services.Order>();
        public MainWindow()
        {
            InitializeComponent();
            LoadContractTypes();

            ApplyDiscountBox.SelectedIndex = 0;
            PaymentMethodBox.SelectedIndex = 0;
        }
        private double selectedOrderBasePrice = 0.0;

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

        private PhoneMaster.Core.Services.Order? BuildOrderFromForm(Client client)
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

            if (ClientTypeBox.SelectedItem == null)
            {
                MessageBox.Show("Select client type.");
                return null;
            }

            string clientType =
                ((ComboBoxItem)ClientTypeBox.SelectedItem).Content.ToString()!;

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

                if (DurationBox.SelectedItem is not ComboBoxItem durationItem)
                {
                    MessageBox.Show("Select contract duration.");
                    return null;
                }

                int duration = int.Parse(durationItem.Content?.ToString() ?? "0");

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

            var order = new PhoneMaster.Core.Services.Order(inventory);
            order.SetPhone(phone);
            order.SetQuantity(quantity);
            order.SetClient(client);
            order.SetContract(contract);
            order.CalculateTotal();

            return order;
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
     
        private void BackFromCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
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

        private void ClientTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadContractTypes();
            UpdateOrderSummary();
        }

        private void ContractTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContractTypeBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string contractType = selectedItem.Content?.ToString() ?? "";

            DurationBox.Items.Clear();
            DurationBox.SelectedIndex = -1;
            DurationLabel.Visibility = Visibility.Collapsed;
            DurationBox.Visibility = Visibility.Collapsed;

            if (contractType == "Phone + SIM Package")
            {
                DurationLabel.Text = "Contract Length (Months)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add(new ComboBoxItem { Content = "12" });
                DurationBox.Items.Add(new ComboBoxItem { Content = "24" });
            }
            else if (contractType == "Hire Contract")
            {
                DurationLabel.Text = "Hire Duration (Years)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add(new ComboBoxItem { Content = "1" });
                DurationBox.Items.Add(new ComboBoxItem { Content = "2" });
            }

            if (DurationBox.Items.Count > 0)
                DurationBox.SelectedIndex = 0;

            UpdateOrderSummary();
        }

        private void LoadContractTypes()
        {
            ContractTypeBox.Items.Clear();

            if (ClientTypeBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string clientType = selectedItem.Content?.ToString() ?? "";

            ContractTypeBox.Items.Add(new ComboBoxItem { Content = "SIM-Free" });
            ContractTypeBox.Items.Add(new ComboBoxItem { Content = "Phone + SIM Package" });

            if (clientType == "Company")
            {
                ContractTypeBox.Items.Add(new ComboBoxItem { Content = "Hire Contract" });
            }

            ContractTypeBox.SelectedIndex = 0;
        }

        private void ContinueOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ClientTypeBox.SelectedItem == null)
            {
                MessageBox.Show("Select client type.");
                return;
            }

            string clientType =
                ((ComboBoxItem)ClientTypeBox.SelectedItem).Content.ToString()!;

            var clientWindow = new ClientDetailsWindow(clientType);
            clientWindow.Owner = this;

            bool? result = clientWindow.ShowDialog();

            if (result != true || clientWindow.CreatedClient == null)
                return;

            var order = BuildOrderFromForm(clientWindow.CreatedClient);

            if (order == null)
                return;

            pendingOrders.Add(order);
            currentOrder = order;

            MessageBox.Show("Order sent to staff. Please go to the desk for payment.");
        }

        private void PendingOrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PendingOrdersGrid.SelectedItem == null)
            {
                ProcessSummaryText.Text = "Select an order to view details.";
                selectedOrderBasePrice = 0.0;
                UpdatePaymentSummary();
                return;
            }

            dynamic selectedOrder = PendingOrdersGrid.SelectedItem;

            ProcessSummaryText.Text =
                $"Client: {selectedOrder.ClientName}\n" +
                $"Phone: {selectedOrder.PhoneName}\n" +
                $"Quantity: {selectedOrder.Quantity}\n" +
                $"Contract: {selectedOrder.ContractType}\n" +
                $"Total Price: £{selectedOrder.TotalPrice:F2}";

            selectedOrderBasePrice = selectedOrder.TotalPrice;

            UpdatePaymentSummary();
        }

        private void ApplyDiscountBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApplyDiscountBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            if (ManagerUsernameLabel == null || ManagerUsernameBox == null ||
                ManagerPasswordLabel == null || ManagerPasswordBox == null ||
                DiscountPercentLabel == null || DiscountPercentBox == null)
                return;

            string applyDiscount = selectedItem.Content?.ToString() ?? "";
            bool showFields = applyDiscount == "Yes";

            ManagerUsernameLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            ManagerUsernameBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            ManagerPasswordLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            ManagerPasswordBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            DiscountPercentLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            DiscountPercentBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            if (!showFields)
            {
                ManagerUsernameBox.Text = "";
                ManagerPasswordBox.Password = "";
                DiscountPercentBox.Text = "";
            }

            UpdatePaymentSummary();
        }

        private void PaymentMethodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaymentMethodBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            if (SortCodeLabel == null || SortCodeBox == null ||
                AccountNumberLabel == null || AccountNumberBox == null)
                return;

            string paymentMethod = selectedItem.Content?.ToString() ?? "";
            bool showCardFields = paymentMethod == "CARD";

            SortCodeLabel.Visibility = showCardFields ? Visibility.Visible : Visibility.Collapsed;
            SortCodeBox.Visibility = showCardFields ? Visibility.Visible : Visibility.Collapsed;

            AccountNumberLabel.Visibility = showCardFields ? Visibility.Visible : Visibility.Collapsed;
            AccountNumberBox.Visibility = showCardFields ? Visibility.Visible : Visibility.Collapsed;

            if (!showCardFields)
            {
                SortCodeBox.Text = "";
                AccountNumberBox.Text = "";
            }
        }

        private void DiscountPercentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePaymentSummary();
        }

        private void UpdatePaymentSummary()
        {
            double basePrice = selectedOrderBasePrice;
            double discountPercent = 0.0;
            double discountAmount = 0.0;
            double totalPayable = basePrice;

            if (ApplyDiscountBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string applyDiscount = selectedItem.Content?.ToString() ?? "";

                if (applyDiscount == "Yes")
                {
                    if (double.TryParse(DiscountPercentBox.Text, out double parsedDiscount))
                    {
                        discountPercent = parsedDiscount;
                    }

                    if (discountPercent < 0)
                        discountPercent = 0;

                    if (discountPercent > 100)
                        discountPercent = 100;

                    discountAmount = basePrice * (discountPercent / 100.0);
                    totalPayable = basePrice - discountAmount;
                }
            }

            BasePriceText.Text = $"Base Price: £{basePrice:F2}";
            DiscountAmountText.Text = $"Discount Amount: £{discountAmount:F2}";
            TotalPayableText.Text = $"Total Payable: £{totalPayable:F2}";
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

                    if (DurationBox.SelectedItem is not ComboBoxItem durationItem)
                    {
                        TotalPriceText.Text = "£0.00";
                        MonthlyCostText.Text = "£0.00";
                        return;
                    }

                    int duration = int.Parse(durationItem.Content?.ToString() ?? "0");
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
            ProcessSummaryText.Text = "Select an order to view details.";
        }

        private void ProcessSelectedOrder_Click(object sender, RoutedEventArgs e)
        {
            var selected = PendingOrdersGrid.SelectedItem as PendingOrderDisplay;

            if (selected == null)
            {
                MessageBox.Show("Select an order first.");
                return;
            }

            if (PaymentMethodBox.SelectedItem == null)
            {
                MessageBox.Show("Select payment method.");
                return;
            }

            string processedBy = ProcessedByBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(processedBy))
            {
                MessageBox.Show("Enter staff username.");
                return;
            }

            string paymentMethod =
                ((ComboBoxItem)PaymentMethodBox.SelectedItem).Content.ToString()!;

            var order = selected.OrderRef;

            order.SetPaymentMethod(paymentMethod);
            order.SetProcessedBy(processedBy);

            order.UpdateInventory();
            order.RecordClient();
            order.AssignUkNumbersIfNeeded();
            order.RecordTransaction();
            order.GenerateReceipt();

            pendingOrders.Remove(order);

            RefreshPendingOrdersGrid();

            ProcessSummaryText.Text = "Select an order to view details.";
            ProcessedByBox.Text = "";
            PaymentMethodBox.SelectedIndex = 0;

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
            ProcessSummaryText.Text = "Select an order to view details.";
        }

        private void BackFromProcessOrders_Click(object sender, RoutedEventArgs e)
        {
            ProcessOrdersPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }
        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            InventoryPanel.Visibility = Visibility.Visible;

            LoadInventoryPhones();
            InventoryActionBox.SelectedIndex = 0;
        }
        private void LoadInventoryPhones()
        {
            InventoryPhonesGrid.ItemsSource = null;
            InventoryPhonesGrid.ItemsSource = inventory.GetPhones();
        }
        private void Transactions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("View Transactions screen");
        }

        // PANEL 4 - UPDATE INVENTORY PANEL BASED ON SELECTION AND ACTION
        private void InventoryPhonesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                return;

            InvManufacturerBox.Text = selectedPhone.Manufacturer;
            InvModelBox.Text = selectedPhone.Model;
            InvStorageBox.Text = selectedPhone.Storage.ToString();
            InvReleaseYearBox.Text = selectedPhone.ReleaseYear.ToString();
            InvPriceBox.Text = selectedPhone.Price.ToString("F2");
            InvStockBox.Text = selectedPhone.Stock.ToString();
        }

        private void SearchInventory_Click(object sender, RoutedEventArgs e)
        {
            string keyword = InventorySearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show("Enter a manufacturer or model to search.");
                return;
            }

            var results = inventory.SearchPhone(keyword);

            InventoryPhonesGrid.ItemsSource = null;
            InventoryPhonesGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                MessageBox.Show("No phones found.");
            }
        }

        private void ShowAllInventory_Click(object sender, RoutedEventArgs e)
        {
            InventorySearchBox.Text = "";
            LoadInventoryPhones();
        }

        private void InventoryActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InventoryActionBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string action = selectedItem.Content?.ToString() ?? "";

            // Default all visible
            InvManufacturerLabel.Visibility = Visibility.Visible;
            InvManufacturerBox.Visibility = Visibility.Visible;
            InvModelLabel.Visibility = Visibility.Visible;
            InvModelBox.Visibility = Visibility.Visible;
            InvPriceLabel.Visibility = Visibility.Visible;
            InvPriceBox.Visibility = Visibility.Visible;
            InvStockLabel.Visibility = Visibility.Visible;
            InvStockBox.Visibility = Visibility.Visible;
            InvReleaseYearLabel.Visibility = Visibility.Visible;
            InvReleaseYearBox.Visibility = Visibility.Visible;
            InvStorageLabel.Visibility = Visibility.Visible;
            InvStorageBox.Visibility = Visibility.Visible;

            if (action == "Add Phone")
            {
                InventoryActionButton.Content = "Add Phone";

                InvManufacturerBox.Text = "";
                InvModelBox.Text = "";
                InvPriceBox.Text = "";
                InvStockBox.Text = "";
                InvReleaseYearBox.Text = "";
                InvStorageBox.Text = "";
            }
            else if (action == "Remove Phone")
            {
                InventoryActionButton.Content = "Remove Phone";

                InvManufacturerLabel.Visibility = Visibility.Collapsed;
                InvManufacturerBox.Visibility = Visibility.Collapsed;
                InvModelLabel.Visibility = Visibility.Collapsed;
                InvModelBox.Visibility = Visibility.Collapsed;
                InvPriceLabel.Visibility = Visibility.Collapsed;
                InvPriceBox.Visibility = Visibility.Collapsed;
                InvStockLabel.Visibility = Visibility.Collapsed;
                InvStockBox.Visibility = Visibility.Collapsed;
                InvReleaseYearLabel.Visibility = Visibility.Collapsed;
                InvReleaseYearBox.Visibility = Visibility.Collapsed;
                InvStorageLabel.Visibility = Visibility.Collapsed;
                InvStorageBox.Visibility = Visibility.Collapsed;
            }
            else if (action == "Update Stock")
            {
                InventoryActionButton.Content = "Update Stock";

                InvManufacturerLabel.Visibility = Visibility.Collapsed;
                InvManufacturerBox.Visibility = Visibility.Collapsed;
                InvModelLabel.Visibility = Visibility.Collapsed;
                InvModelBox.Visibility = Visibility.Collapsed;
                InvPriceLabel.Visibility = Visibility.Collapsed;
                InvPriceBox.Visibility = Visibility.Collapsed;
                InvReleaseYearLabel.Visibility = Visibility.Collapsed;
                InvReleaseYearBox.Visibility = Visibility.Collapsed;
                InvStorageLabel.Visibility = Visibility.Collapsed;
                InvStorageBox.Visibility = Visibility.Collapsed;
            }
            else if (action == "Change Price")
            {
                InventoryActionButton.Content = "Change Price";

                InvManufacturerLabel.Visibility = Visibility.Collapsed;
                InvManufacturerBox.Visibility = Visibility.Collapsed;
                InvModelLabel.Visibility = Visibility.Collapsed;
                InvModelBox.Visibility = Visibility.Collapsed;
                InvStockLabel.Visibility = Visibility.Collapsed;
                InvStockBox.Visibility = Visibility.Collapsed;
                InvReleaseYearLabel.Visibility = Visibility.Collapsed;
                InvReleaseYearBox.Visibility = Visibility.Collapsed;
                InvStorageLabel.Visibility = Visibility.Collapsed;
                InvStorageBox.Visibility = Visibility.Collapsed;
            }
        }
        private void InventoryActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (InventoryActionBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("Select an inventory action.");
                return;
            }

            string action = selectedItem.Content?.ToString() ?? "";

            if (action == "Add Phone")
            {
                string manufacturer = InvManufacturerBox.Text.Trim();
                string model = InvModelBox.Text.Trim();


                if (string.IsNullOrWhiteSpace(manufacturer))
                {
                    MessageBox.Show("Enter phone manufacturer.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(model))
                {
                    MessageBox.Show("Enter phone model.");
                    return;
                }

                if (!int.TryParse(InvStorageBox.Text, out int storage) || storage <= 0)
                {
                    MessageBox.Show("Enter a valid storage capacity.");
                    return;
                }

                if (!int.TryParse(InvReleaseYearBox.Text, out int releaseYear) || releaseYear <= 1990)
                {
                    MessageBox.Show("Enter a valid release year.");
                    return;
                }

                if (!double.TryParse(InvPriceBox.Text, out double price) || price < 0)
                {
                    MessageBox.Show("Enter a valid phone price.");
                    return;
                }

                if (!int.TryParse(InvStockBox.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Enter a valid initial stock.");
                    return;
                }

                string newPhoneId = inventory.GenerateNextPhoneID();

                Phone newPhone = new Phone(
                    newPhoneId,
                    manufacturer,
                    model,
                    storage,
                    releaseYear,
                    price,
                    stock
                );

                inventory.AddPhone(newPhone);

                LoadInventoryPhones();

                InvManufacturerBox.Text = "";
                InvModelBox.Text = "";
                InvStorageBox.Text = "";
                InvReleaseYearBox.Text = "";
                InvPriceBox.Text = "";
                InvStockBox.Text = "";

                MessageBox.Show("Phone added successfully.");
            }
            else if (action == "Remove Phone")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to remove.");
                    return;
                }

                bool removed = inventory.RemovePhone(selectedPhone.PhoneID);

                if (!removed)
                {
                    MessageBox.Show("Phone could not be removed.");
                    return;
                }

                LoadInventoryPhones();
                MessageBox.Show("Phone removed successfully.");
            }
            else if (action == "Update Stock")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to update stock.");
                    return;
                }

                if (!int.TryParse(InvStockBox.Text, out int newStock) || newStock < 0)
                {
                    MessageBox.Show("Enter a valid stock value.");
                    return;
                }

                bool updated = inventory.UpdateStock(selectedPhone.PhoneID, newStock);

                if (!updated)
                {
                    MessageBox.Show("Stock could not be updated.");
                    return;
                }

                LoadInventoryPhones();
                MessageBox.Show("Stock updated successfully.");
            }
            else if (action == "Change Price")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to change price.");
                    return;
                }

                if (!double.TryParse(InvPriceBox.Text, out double newPrice) || newPrice < 0)
                {
                    MessageBox.Show("Enter a valid price.");
                    return;
                }

                bool updated = inventory.ChangePrice(selectedPhone.PhoneID, newPrice);

                if (!updated)
                {
                    MessageBox.Show("Price could not be changed.");
                    return;
                }

                LoadInventoryPhones();
                MessageBox.Show("Price changed successfully.");
            }
        }
        
        private void BackFromInventory_Click(object sender, RoutedEventArgs e)
        {
            InventoryPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
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

        // Helpers 
        private void TriggerButtonOnEnter(System.Windows.Input.KeyEventArgs e, RoutedEventHandler action)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                action.Invoke(this, new RoutedEventArgs());
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            TriggerButtonOnEnter(e, SearchPhone_Click);
        }

        private void InventorySearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            TriggerButtonOnEnter(e, SearchInventory_Click);
        }
    }
}