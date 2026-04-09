using Microsoft.Data.Sqlite;
using PhoneMaster.Core.Models;
using PhoneMaster.Core.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace PhoneMaster.GUI
{

    
    public class ClientDisplay
    {
        public string ClientType { get; set; } = "";
        public string Name { get; set; } = "";
        public string VAT { get; set; } = "";
        public string Email { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public string Address { get; set; } = "";
    }

    public class InventoryLogDisplay
    {
        public string Timestamp { get; set; } = "";
        public string PerformedBy { get; set; } = "";
        public string Action { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Details { get; set; } = "";
    }
    public partial class MainWindow : Window
    {
        private Inventory? inventory;
        private Staff? currentUser;
        private PhoneMaster.Core.Services.Order? currentOrder;
        private List<PhoneMaster.Core.Services.Order> pendingOrders = new List<PhoneMaster.Core.Services.Order>();
        private double selectedOrderBasePrice = 0.0;
        private bool discountValidated = false;
        private double validatedDiscountPercent = 0.0;

        public MainWindow()
        {
            InitializeComponent();
            IdleMode();
        }
     

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

            DatabaseManager.InitializeDatabase();

            inventory = new Inventory();
            inventory.LoadPhones();

            AppMenu();

        }


        private void IdleMode()
        {
            HideAllPanels();
            WelcomePanel.Visibility = Visibility.Visible;

            currentUser = null;
            currentOrder = null;
            selectedOrderBasePrice = 0.0;

            ResetDiscountApprovalState();

            PaymentMethodBox.SelectedIndex = -1;
            PaymentOptionBox.SelectedIndex = -1;
            ApplyDiscountBox.SelectedIndex = 0;

            ProcessedByBox.Text = "";
            SortCodeBox.Text = "";
            AccountNumberBox.Text = "";

            ProcessSummaryText.Text = "Select an order to view details.";

            ShowTransactionsWelcome();
        }


        private void AppMenu()
        {
            HideAllPanels();
            MenuPanel.Visibility = Visibility.Visible;
        }


        private void HideAllPanels()
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Collapsed;
            PhonesPanel.Visibility = Visibility.Collapsed;
            CreateOrderPanel.Visibility = Visibility.Collapsed;
            ProcessOrdersPanel.Visibility = Visibility.Collapsed;
            InventoryPanel.Visibility = Visibility.Collapsed;
            TransactionsPanel.Visibility = Visibility.Collapsed;
        }

        
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            IdleMode();
        }



        //PANEL 1 - PHONES PANEL

        private void Phones_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            PhonesPanel.Visibility = Visibility.Visible;
            LoadPhones();
        }


        private void LoadPhones()
        {
            PhonesGrid.ItemsSource = null;
            PhonesGrid.ItemsSource = inventory.GetPhones();
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


        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            TriggerButtonOnEnter(e, SearchPhone_Click);
        }


        private void ShowAllPhones_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            LoadPhones();
        }


        private void PhonesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhonesGrid.SelectedItem is not Phone selectedPhone)
            {
                SelectedPhoneText.Text = "No phone selected";
                SelectedPhoneImage.Source = null;
                return;
            }

            SelectedPhoneText.Text =

                $"{selectedPhone.Manufacturer} {selectedPhone.Model}\n" +
                $"Storage: {selectedPhone.Storage} GB\n" +
                $"Release Year: {selectedPhone.ReleaseYear}\n" +
                $"Price: £{selectedPhone.Price:F2}\n";
        }

        private void BackFromPhones_Click(object sender, RoutedEventArgs e)
        {
            PhonesPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }



        // PANEL 2 - CREATE ORDER PANEL
        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed;
            CreateOrderPanel.Visibility = Visibility.Visible;

            CreateOrderPhonesGrid.ItemsSource = null;
            CreateOrderPhonesGrid.ItemsSource = inventory.GetPhones();
        }


        private void LoadCreateOrderPhones()
        {
            CreateOrderPhonesGrid.ItemsSource = null;
            CreateOrderPhonesGrid.ItemsSource = inventory.GetPhones();
        }

        private void SearchCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            string searchText = CreateOrderSearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Enter manufacturer or model to search.");
                return;
            }

            var filteredPhones = inventory.GetPhones()
                .Where(p =>
                    p.Manufacturer.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Model.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            CreateOrderPhonesGrid.ItemsSource = filteredPhones;

            if (filteredPhones.Count == 0)
            {
                MessageBox.Show("No matching phones found.");
            }
        }


        private void CreateOrderSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            TriggerButtonOnEnter(e, SearchCreateOrder_Click);
        }


        private void ShowAllCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderSearchBox.Text = "";
            LoadCreateOrderPhones();
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
                MessageBox.Show("Please select a Client Type.");
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

            // Reset plan controls
            PlanTypeLabel.Visibility = Visibility.Collapsed;
            PlanTypeBox.Visibility = Visibility.Collapsed;
            PlanTypeBox.SelectedIndex = -1;

            // Reset duration controls
            DurationBox.Items.Clear();
            DurationBox.SelectedIndex = -1;
            DurationLabel.Visibility = Visibility.Collapsed;
            DurationBox.Visibility = Visibility.Collapsed;

            if (contractType == "Phone + SIM Package")
            {
                PlanTypeLabel.Visibility = Visibility.Visible;
                PlanTypeBox.Visibility = Visibility.Visible;

                DurationLabel.Text = "Contract Length (Months)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add(new ComboBoxItem { Content = "12" });
                DurationBox.Items.Add(new ComboBoxItem { Content = "24" });

                if (PlanTypeBox.Items.Count > 0)
                    PlanTypeBox.SelectedIndex = 0;

                DurationBox.SelectedIndex = 0;
            }
            else if (contractType == "Hire Contract")
            {
                PlanTypeLabel.Visibility = Visibility.Visible;
                PlanTypeBox.Visibility = Visibility.Visible;

                DurationLabel.Text = "Hire Duration (Years)";
                DurationLabel.Visibility = Visibility.Visible;
                DurationBox.Visibility = Visibility.Visible;

                DurationBox.Items.Add(new ComboBoxItem { Content = "1" });
                DurationBox.Items.Add(new ComboBoxItem { Content = "2" });

                if (PlanTypeBox.Items.Count > 0)
                    PlanTypeBox.SelectedIndex = 0;

                DurationBox.SelectedIndex = 0;
            }

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

            if (CreateOrderPhonesGrid.SelectedItem is not Phone selectedPhone)
            {
                MessageBox.Show("Select a phone first.");
                return;
            }


            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Enter a valid quantity.");
                QuantityTextBox.Focus();
                return;
            }

          
            if (quantity > selectedPhone.Stock)
            {
                MessageBox.Show($"Not enough stock available. Only {selectedPhone.Stock} left.");
                QuantityTextBox.Focus();
                return;
            }

            if (ClientTypeBox.SelectedItem == null)
            {
                MessageBox.Show("Please select the Client Type.");
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

            ResetCreateOrderPanel();

            MessageBox.Show("Order sent to staff. Please go to the desk for payment.");
            
        }


        public void ResetCreateOrderPanel()
        {
            CreateOrderSearchBox.Text = "";
            LoadCreateOrderPhones();

            CreateOrderPhonesGrid.SelectedItem = null;

            QuantityTextBox.Text = "";

            ClientTypeBox.SelectedIndex = -1;
            ContractTypeBox.Items.Clear();
            ContractTypeBox.SelectedIndex = -1;

            PlanTypeBox.SelectedIndex = -1;
            PlanTypeBox.Visibility = Visibility.Collapsed;
            PlanTypeLabel.Visibility = Visibility.Collapsed;

            DurationBox.Items.Clear();
            DurationBox.SelectedIndex = -1;
            DurationBox.Visibility = Visibility.Collapsed;
            DurationLabel.Visibility = Visibility.Collapsed;

            PhoneDetailsText.Text = "-";
            TotalPriceText.Text = "£0.00";
            MonthlyCostText.Text = "£0.00";
        }

        private void BackFromCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }



        // PANEL 3 - PROCESS ORDER PANEL
        private void ProcessOrder_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow login = new AuthWindow();

            if (login.ShowDialog() != true)
                return;

            var staff = login.LoggedUser;


            if (staff == null || !staff.CanProcessOrders())
            {
                MessageBox.Show("Access denied. Only STAFF or MANAGER allowed.");
                return;
            }
            currentUser = staff;

            MenuPanel.Visibility = Visibility.Collapsed;
            ProcessOrdersPanel.Visibility = Visibility.Visible;

            RefreshPendingOrdersGrid();
            ProcessSummaryText.Text = "Select an order to view details.";


            ProcessedByBox.Text = currentUser?.Username ?? "";
        }


        private void RefreshPendingOrdersGrid()
        {
            var displayList = pendingOrders.Select(order => new PendingOrderDisplay
            {
                OrderRef = order,
                ClientName = order.GetClient()?.Name ?? "",
                PhoneName = order.GetPhone() == null
                    ? ""
                    : $"{order.GetPhone()!.Manufacturer} {order.GetPhone()!.Model} {order.GetPhone()!.Storage} GB",
                Quantity = order.GetQuantity(),
                ContractType = order.GetContract()?.GetName() ?? "",
                TotalPrice = order.GetTotalAfterDiscount()
            }).ToList();

            PendingOrdersGrid.ItemsSource = null;
            PendingOrdersGrid.ItemsSource = displayList;
        }


        private void PendingOrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset discount/payment state when switching orders
            ApplyDiscountBox.SelectedIndex = 0;
            ResetDiscountApprovalState();
            ManagerUsernameBox.Text = "";
            ManagerPasswordBox.Password = "";
            DiscountPercentBox.Text = "";
            discountValidated = false;
            validatedDiscountPercent = 0.0;

            PaymentMethodBox.SelectedIndex = -1;
            PaymentOptionBox.SelectedIndex = -1;
            SortCodeBox.Text = "";
            AccountNumberBox.Text = "";

            if (PendingOrdersGrid.SelectedItem is not PendingOrderDisplay selectedOrder)
            {
                selectedOrderBasePrice = 0.0;
                ProcessSummaryText.Text = "Select an order to view details.";
                UpdatePaymentSummary();
                return;
            }

            var order = selectedOrder.OrderRef;
            var phone = order.GetPhone();
            var contract = order.GetContract();

            string phoneDetails = phone == null
                ? "-"
                : $"{phone.Manufacturer} {phone.Model} {phone.Storage}GB - £{phone.Price:F2}";

            string planTypeText = "-";
            string durationText = "-";

            if (contract is PhoneSimPackage package)
            {
                planTypeText = package.PlanType == PlanType.STANDARD ? "Standard Plan" : "Premium Plan";
                durationText = $"{package.Months} months";
            }
            else if (contract is HireContract hire)
            {
                planTypeText = hire.PlanType == PlanType.STANDARD ? "Standard Plan" : "Premium Plan";
                durationText = $"{hire.Years} years";
            }
            else if (contract is SimFree)
            {
                planTypeText = "SIM Free";
                durationText = "-";
            }

            ProcessSummaryText.Text =
                $"Client: {selectedOrder.ClientName}\n" +
                $"{phoneDetails}\n" +
                $"Quantity: {selectedOrder.Quantity}\n" +
                $"Contract: {selectedOrder.ContractType}\n" +
                $"Plan Type: {planTypeText}\n" +
                $"Duration: {durationText}\n" +
                $"Total Contract Price: £{selectedOrder.TotalPrice:F2}";

            selectedOrderBasePrice = selectedOrder.TotalPrice;

            try
            {
                order.CalculateTotal();
                selectedOrderBasePrice = order.GetTotalAfterDiscount();
            }
            catch
            {
                selectedOrderBasePrice = 0.0;
            }

            UpdatePaymentSummary();
        }


        private void ApplyDiscountBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApplyDiscountBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string applyDiscount = selectedItem.Content?.ToString() ?? "";
            bool showFields = applyDiscount == "Yes";

            ManagerUsernameLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            ManagerUsernameBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            ManagerPasswordLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            ManagerPasswordBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            DiscountPercentLabel.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
            DiscountPercentBox.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            ValidateDiscountButton.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;

            ResetDiscountApprovalState();

            UpdatePaymentSummary();
        }


        private void SetDiscountControlsLocked(bool isLocked)
        {
            ManagerUsernameBox.IsEnabled = !isLocked;
            ManagerPasswordBox.IsEnabled = !isLocked;
            DiscountPercentBox.IsEnabled = !isLocked;
            ValidateDiscountButton.IsEnabled = !isLocked;
        }


        private void ResetDiscountApprovalState()
        {
            discountValidated = false;
            validatedDiscountPercent = 0.0;

            ManagerUsernameBox.Text = "";
            ManagerPasswordBox.Password = "";
            DiscountPercentBox.Text = "";

            SetDiscountControlsLocked(false);
        }


        private void ValidateDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            if (PendingOrdersGrid.SelectedItem is not PendingOrderDisplay selectedOrder)
            {
                MessageBox.Show("Select an order first.");
                return;
            }

            string username = ManagerUsernameBox.Text.Trim();
            string password = ManagerPasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter manager username and password.");
                return;
            }

            var manager = Staff.LoginFromFile(username, password);

            if (manager == null || !manager.CanApproveDiscount())
            {
                MessageBox.Show("Manager authentication failed. Discount was not approved.");

                ApplyDiscountBox.SelectedIndex = 0; // No
                ResetDiscountApprovalState();
                UpdatePaymentSummary();
                return;
            }

            if (!double.TryParse(DiscountPercentBox.Text, out double discountPercent))
            {
                MessageBox.Show("Enter a valid discount percentage.");
                return;
            }

            if (discountPercent < 1 || discountPercent > 20)
            {
                MessageBox.Show("Discount must be between 1% and 20%.");
                return;
            }

            discountValidated = true;
            validatedDiscountPercent = discountPercent;

            SetDiscountControlsLocked(true);
            UpdatePaymentSummary();

            MessageBox.Show("Discount approved successfully.");
        }
       

        private void PaymentMethodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isCard = false;

            if (PaymentMethodBox.SelectedItem is ComboBoxItem item)
            {
                isCard = item.Content?.ToString() == "CARD";
            }

            SortCodeLabel.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;
            SortCodeBox.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;

            AccountNumberLabel.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;
            AccountNumberBox.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;

            bool showPaymentOption = false;

            if (PendingOrdersGrid.SelectedItem is PendingOrderDisplay selectedOrder)
            {
                var contract = selectedOrder.OrderRef.GetContract();
                showPaymentOption = isCard &&
                                    (contract is PhoneSimPackage || contract is HireContract);
            }

            PaymentOptionLabel.Visibility = showPaymentOption ? Visibility.Visible : Visibility.Collapsed;
            PaymentOptionBox.Visibility = showPaymentOption ? Visibility.Visible : Visibility.Collapsed;

            if (!showPaymentOption)
            {
                PaymentOptionBox.SelectedIndex = -1;
            }

            if (!isCard)
            {
                SortCodeBox.Text = "";
                AccountNumberBox.Text = "";
            }
        }


        private void SortCodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string digits = new string(SortCodeBox.Text.Where(char.IsDigit).ToArray());

            if (digits.Length > 6)
                digits = digits.Substring(0, 6);

            string formatted = "";

            if (digits.Length >= 2)
                formatted += digits.Substring(0, 2);
            else
                formatted += digits;

            if (digits.Length >= 4)
                formatted += "-" + digits.Substring(2, 2);
            else if (digits.Length > 2)
                formatted += "-" + digits.Substring(2);

            if (digits.Length > 4)
                formatted += "-" + digits.Substring(4);

            SortCodeBox.TextChanged -= SortCodeBox_TextChanged;
            SortCodeBox.Text = formatted;
            SortCodeBox.CaretIndex = SortCodeBox.Text.Length;
            SortCodeBox.TextChanged += SortCodeBox_TextChanged;
        }

        private void PaymentOptionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePaymentSummary();
        }

        private void DiscountPercentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        UpdatePaymentSummary();
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
                MessageBox.Show("Please select a Payment method.");
                return;
            }

            string processedBy = ProcessedByBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(processedBy))
            {
                MessageBox.Show("Enter staff username.");
                return;
            }

            var order = selected.OrderRef;
            var contract = order.GetContract();

            string paymentMethod =
                ((ComboBoxItem)PaymentMethodBox.SelectedItem).Content.ToString()!;

            if (paymentMethod == "CARD")
            {
                string accountNumber = AccountNumberBox.Text.Trim();
                string sortCode = SortCodeBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(accountNumber) && string.IsNullOrWhiteSpace(sortCode))
                {
                    MessageBox.Show("Sort Code and Account Number are required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    MessageBox.Show("Account Number is required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(sortCode))
                {
                    MessageBox.Show("Sort Code is required.");
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(accountNumber, @"^\d{8}$"))
                {
                    MessageBox.Show("Account Number must be exactly 8 digits.");
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(sortCode, @"^\d{2}-\d{2}-\d{2}$"))
                {
                    MessageBox.Show("Sort Code must be in format 12-34-56.");
                    return;
                }

                
            }

            // Require payment option only for PhoneSimPackage and HireContract when payment is CARD
            if ((contract is PhoneSimPackage || contract is HireContract) &&
                paymentMethod == "CARD" &&
                PaymentOptionBox.SelectedItem == null)
            {
                MessageBox.Show("Select a Payment option.");
                return;
            }

            string paymentOption = "";
            if (PaymentOptionBox.SelectedItem is ComboBoxItem paymentOptionItem)
            {
                paymentOption = paymentOptionItem.Content.ToString()!;
            }

            // Discount validation logic
            double approvedDiscount = 0.0;

            if (ApplyDiscountBox.SelectedItem is ComboBoxItem discountItem &&
                discountItem.Content?.ToString() == "Yes")
            {
                if (!discountValidated)
                {
                    MessageBox.Show("Discount must be validated by a manager before processing.");
                    return;
                }

                approvedDiscount = validatedDiscountPercent;
            }

            if (!order.ApplyDiscount(approvedDiscount))
            {
                MessageBox.Show("Discount could not be applied.");
                return;
            }

            order.CalculateTotal();
            selectedOrderBasePrice = order.GetTotalAfterDiscount();
            UpdatePaymentSummary();

            order.SetPaymentMethod(paymentMethod);
            order.SetProcessedBy(processedBy);

            if (paymentMethod == "CARD" && paymentOption == "Monthly Instalments")
            {
                order.SetMonthlyPayment(true);
                order.SetMonthlyAmount(order.CalculateMonthlyPayment());
            }
            else
            {
                order.SetMonthlyPayment(false);
                order.SetMonthlyAmount(0);
            }

            if (paymentMethod == "CARD")
            {
                order.SetCardDetails(AccountNumberBox.Text.Trim(), SortCodeBox.Text.Trim());
            }

            // Final confirmation dialog before final processing
            var phone = order.GetPhone();
            var client = order.GetClient();

            string confirmationMessage =
                $"Are you sure you want to process this order?\n\n" +
                $"Client: {client?.Name ?? "-"}\n" +
                $"Phone: {phone?.Manufacturer} {phone?.Model} {phone?.Storage}GB\n" +
                $"Quantity: {order.GetQuantity()}\n" +
                $"Payment Method: {paymentMethod}\n" +
                $"Total Payable: £{order.GetTotalAfterDiscount():F2}";

            if (paymentMethod == "CARD" && paymentOption == "Monthly Instalments")
            {
                confirmationMessage += $"\nMonthly Instalment: £{order.CalculateMonthlyPayment():F2}";
            }

            if (approvedDiscount > 0)
            {
                confirmationMessage += $"\nDiscount Approved: {approvedDiscount:F0}%";
            }

            MessageBoxResult confirmResult = MessageBox.Show(
                confirmationMessage,
                "Confirm Process Order",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            order.UpdateInventory();
            order.RecordClient();
            order.AssignUkNumbersIfNeeded();
            order.RecordTransaction();
            order.GenerateReceipt();

            pendingOrders.Remove(order);

            RefreshPendingOrdersGrid();

            ProcessSummaryText.Text = "Select an order to view details.";
            ProcessedByBox.Text = currentUser?.Username ?? "";

            PaymentMethodBox.SelectedIndex = -1;
            PaymentOptionBox.SelectedIndex = -1;

            SortCodeBox.Text = "";
            AccountNumberBox.Text = "";

            ApplyDiscountBox.SelectedIndex = 0;
            ResetDiscountApprovalState();

            selectedOrderBasePrice = 0.0;
            UpdatePaymentSummary();

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

            var order = selected.OrderRef;
            var phone = order.GetPhone();
            var client = order.GetClient();

            string orderDetails =

                $"Client: {client?.Name ?? "-"}\n" +
                $"Phone: {phone?.Manufacturer} {phone?.Model} {phone?.Storage}GB";

            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete this order?\n\n{orderDetails}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            pendingOrders.Remove(order);

            RefreshPendingOrdersGrid();

            ProcessSummaryText.Text = "Select an order to view details.";

            MessageBox.Show("Order deleted successfully.");
        }


        private void BackFromProcessOrders_Click(object sender, RoutedEventArgs e)
        {
            ProcessOrdersPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }



        // PANEL 4 - UPDATE INVENTORY PANEL BASED ON SELECTION AND ACTION
        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow login = new AuthWindow();

            if (login.ShowDialog() != true)
                return;

            var staff = login.LoggedUser;

            if (staff == null || !staff.CanUpdateInventory())
            {
                MessageBox.Show("Access denied. CENTRAL role required.");
                return;
            }

            currentUser = staff;

            MenuPanel.Visibility = Visibility.Collapsed;
            InventoryPanel.Visibility = Visibility.Visible;

            LoadInventoryPhones();
        }


        private void LoadInventoryPhones()
        {
            InventoryPhonesGrid.ItemsSource = null;
            InventoryPhonesGrid.ItemsSource = inventory.GetPhones();
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


        private void InventorySearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            TriggerButtonOnEnter(e, SearchInventory_Click);
        }


        private void ShowAllInventory_Click(object sender, RoutedEventArgs e)
        {
            InventorySearchBox.Text = "";
            LoadInventoryPhones();
        }


        private void InventoryPhonesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
            {
                InvManufacturerBox.Text = "";
                InvModelBox.Text = "";
                InvStorageBox.Text = "";
                InvReleaseYearBox.Text = "";
                InvPriceBox.Text = "";
                InvOldStockBox.Text = "";

                if (InvSelectedPhoneLabel.Visibility == Visibility.Visible)
                    InvSelectedPhoneLabel.Text = "Selected phone: none";

                return;
            }

            InvManufacturerBox.Text = selectedPhone.Manufacturer;
            InvModelBox.Text = selectedPhone.Model;
            InvStorageBox.Text = selectedPhone.Storage.ToString();
            InvReleaseYearBox.Text = selectedPhone.ReleaseYear.ToString();
            InvPriceBox.Text = selectedPhone.Price.ToString("F2");
            InvOldStockBox.Text = selectedPhone.Stock.ToString();

            if (InvSelectedPhoneLabel.Visibility == Visibility.Visible)
            {
                InvSelectedPhoneLabel.Text =
                                        $"Selected phone:\n" +
                                        $"{selectedPhone.PhoneID} - {selectedPhone.Manufacturer} {selectedPhone.Model}\n" +
                                        $"{selectedPhone.Storage}GB | £{selectedPhone.Price:F2} | Stock: {selectedPhone.Stock}";
            }
        }

        private void ClearInventoryInputs()
        {
            InvNewStockBox.Text = "";
            InvNewPriceBox.Text = "";
        }

        private void ClearAddPhoneFields()
        {
            InvManufacturerBox.Text = "";
            InvModelBox.Text = "";
            InvStorageBox.Text = "";
            InvReleaseYearBox.Text = "";
            InvPriceBox.Text = "";
            InvInitialStockBox.Text = "";

            InvNewPriceBox.Text = "";
            InvCurrentPriceBox.Text = "";
            InvOldStockBox.Text = "";
            InvNewStockBox.Text = "";
        }


        private void NumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PriceInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^\d{0,5}(\.\d{0,2})?$");

            TextBox textBox = sender as TextBox;

            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            e.Handled = !regex.IsMatch(newText);
        }


        private void InventoryActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InventoryActionBox.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string action = selectedItem.Content?.ToString() ?? "";

            InvManufacturerLabel.Visibility = Visibility.Collapsed;
            InvManufacturerBox.Visibility = Visibility.Collapsed;

            InvModelLabel.Visibility = Visibility.Collapsed;
            InvModelBox.Visibility = Visibility.Collapsed;

            InvStorageLabel.Visibility = Visibility.Collapsed;
            InvStorageBox.Visibility = Visibility.Collapsed;

            InvReleaseYearLabel.Visibility = Visibility.Collapsed;
            InvReleaseYearBox.Visibility = Visibility.Collapsed;

            InvPriceLabel.Visibility = Visibility.Collapsed;
            InvPriceBox.Visibility = Visibility.Collapsed;

            InvInitialStockLabel.Visibility = Visibility.Collapsed;
            InvInitialStockBox.Visibility = Visibility.Collapsed;

            InvOldStockLabel.Visibility = Visibility.Collapsed;
            InvOldStockBox.Visibility = Visibility.Collapsed;

            InvNewStockLabel.Visibility = Visibility.Collapsed;
            InvNewStockBox.Visibility = Visibility.Collapsed;

            InvNewPriceLabel.Visibility = Visibility.Collapsed;
            InvNewPriceBox.Visibility = Visibility.Collapsed;

            InvSelectedPhoneLabel.Visibility = Visibility.Collapsed;
            InvSelectedPhoneLabel.Text = "Selected phone: none";

            // Add Phone
            if (action == "Add Phone")
            {
                ClearAddPhoneFields();
                InventoryPhonesGrid.SelectedItem = null;

                InventoryActionButton.Content = "Add Phone";

                InvManufacturerLabel.Visibility = Visibility.Visible;
                InvManufacturerBox.Visibility = Visibility.Visible;

                InvModelLabel.Visibility = Visibility.Visible;
                InvModelBox.Visibility = Visibility.Visible;

                InvStorageLabel.Visibility = Visibility.Visible;
                InvStorageBox.Visibility = Visibility.Visible;

                InvReleaseYearLabel.Visibility = Visibility.Visible;
                InvReleaseYearBox.Visibility = Visibility.Visible;

                InvPriceLabel.Visibility = Visibility.Visible;
                InvPriceBox.Visibility = Visibility.Visible;
                InvPriceBox.IsReadOnly = false;

                InvInitialStockLabel.Visibility = Visibility.Visible;
                InvInitialStockBox.Visibility = Visibility.Visible;
            }
            else if (action == "Remove Phone")
            {
                InventoryActionButton.Content = "Remove Phone";

                InvSelectedPhoneLabel.Visibility = Visibility.Visible;
            }
            else if (action == "Update Stock")
            {
                InventoryActionButton.Content = "Update Stock";

                InvOldStockLabel.Visibility = Visibility.Visible;
                InvOldStockBox.Visibility = Visibility.Visible;

                InvNewStockLabel.Visibility = Visibility.Visible;
                InvNewStockBox.Visibility = Visibility.Visible;

                InvNewStockBox.Text = "";
                InvSelectedPhoneLabel.Visibility = Visibility.Visible;
            }
            else if (action == "Change Price")
            {
                InventoryActionButton.Content = "Change Price";

                InvPriceLabel.Visibility = Visibility.Visible;
                InvPriceBox.Visibility = Visibility.Visible;
                InvPriceBox.IsReadOnly = true;

                InvNewPriceLabel.Visibility = Visibility.Visible;
                InvNewPriceBox.Visibility = Visibility.Visible;

                InvNewPriceBox.Text = "";
                InvSelectedPhoneLabel.Visibility = Visibility.Visible;
            }

            if (InvSelectedPhoneLabel.Visibility == Visibility.Visible &&
                InventoryPhonesGrid.SelectedItem is Phone selectedPhone)
            {
                InvSelectedPhoneLabel.Text =
                $"Selected phone:\n" +
                $"{selectedPhone.PhoneID} - {selectedPhone.Manufacturer} {selectedPhone.Model}\n" +
                $"{selectedPhone.Storage}GB | £{selectedPhone.Price:F2} | Stock: {selectedPhone.Stock}";
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

            // Add Phone
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
                
                if (InvStorageBox.SelectedItem is not ComboBoxItem storageItem)
                {
                    MessageBox.Show("Select storage capacity.");
                    return;
                }

                int storage = int.Parse(storageItem.Content.ToString());

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

                if (!int.TryParse(InvInitialStockBox.Text, out int stock) || stock < 0 || stock > 100)
                {
                    MessageBox.Show("Initial stock must be between 0 and 100.");
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

                bool added = inventory.AddPhone(newPhone, currentUser?.Username ?? "UNKNOWN");

                if (!added)
                {
                    MessageBox.Show("Stock must be between 0 and 100.");
                    return;
                }

                LoadInventoryPhones();

                InvManufacturerBox.Text = "";
                InvModelBox.Text = "";
                InvStorageBox.SelectedIndex = -1;
                InvReleaseYearBox.Text = "";
                InvPriceBox.Text = "";
                InvInitialStockBox.Text = "";
                InvOldStockBox.Text = "";
                InvNewStockBox.Text = "";

                MessageBox.Show("Phone added successfully.");
            }

            // Remove Phone
            else if (action == "Remove Phone")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to remove.");
                    return;
                }

                string phoneName = $"{selectedPhone.Manufacturer} {selectedPhone.Model} {selectedPhone.Storage}GB";

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete this phone?\n\n{phoneName}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                bool removed = inventory.RemovePhone(selectedPhone.PhoneID, currentUser?.Username ?? "UNKNOWN");

                if (!removed)
                {
                    MessageBox.Show("Phone could not be removed.");
                    return;
                }

                LoadInventoryPhones();
                MessageBox.Show("Phone removed successfully.");
            }

            // Update Stock
            else if (action == "Update Stock")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to update stock.");
                    return;
                }

                if (!int.TryParse(InvNewStockBox.Text, out int newStock))
                {
                    MessageBox.Show("Enter a valid stock number.");
                    return;
                }

                string phoneName = $"{selectedPhone.Manufacturer} {selectedPhone.Model} {selectedPhone.Storage}GB";

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to update stock?\n\n{phoneName}\nNew Stock: {newStock}",
                    "Confirm Update Stock",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                bool updated = inventory.UpdateStock(selectedPhone.PhoneID, newStock, currentUser?.Username ?? "UNKNOWN");

                if (!updated)
                {
                    MessageBox.Show("Stock must be between 0 and 100.");
                    return;
                }

                LoadInventoryPhones();

                ClearInventoryInputs();

                MessageBox.Show("Stock updated successfully.");
            }

            // Change Price
            else if (action == "Change Price")
            {
                if (InventoryPhonesGrid.SelectedItem is not Phone selectedPhone)
                {
                    MessageBox.Show("Select a phone to change price.");
                    return;
                }

                if (!double.TryParse(InvNewPriceBox.Text, out double newPrice) || newPrice <= 0)
                {
                    MessageBox.Show("Enter a valid price.");
                    return;
                }
                
                string phoneName = $"{selectedPhone.Manufacturer} {selectedPhone.Model} {selectedPhone.Storage}GB";

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to change price?\n\n{phoneName}\nNew Price: £{newPrice:F2}",
                    "Confirm Price Change",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                bool updated = inventory.ChangePrice(selectedPhone.PhoneID, newPrice, currentUser?.Username ?? "UNKNOWN");

                if (!updated)
                {
                    MessageBox.Show("Price could not be changed.");
                    return;
                }

                LoadInventoryPhones();
                ClearInventoryInputs();
                MessageBox.Show("Price changed successfully.");
            }
        }

        private void BackFromInventory_Click(object sender, RoutedEventArgs e)
        {
            InventoryPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;

            currentUser = null;
        }



        // 5 - VIEW TRANSACTION PANEL
        private void Transactions_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow login = new AuthWindow();

            if (login.ShowDialog() != true)
                return;

            var staff = login.LoggedUser;

            if (staff == null || !staff.CanViewTransactions())
            {
                MessageBox.Show("Access denied.");
                return;
            }

            MenuPanel.Visibility = Visibility.Collapsed;
            TransactionsPanel.Visibility = Visibility.Visible;
            ShowTransactionsWelcome();
        }


        private void ShowTransactionsWelcome()
        {
            TransactionsWelcomePanel.Visibility = Visibility.Visible;
            ShopBalancePanel.Visibility = Visibility.Collapsed;
            OrderHistoryPanel.Visibility = Visibility.Collapsed;
            ClientsPanel.Visibility = Visibility.Collapsed;
        }

        private void HideAllTransactionsSubPanels()
        {
            TransactionsWelcomePanel.Visibility = Visibility.Collapsed;
            ShopBalancePanel.Visibility = Visibility.Collapsed;
            OrderHistoryPanel.Visibility = Visibility.Collapsed;
            ClientsPanel.Visibility = Visibility.Collapsed;
            InventoryLogPanel.Visibility = Visibility.Collapsed;
        }


        // View Shop Balance
        private void ViewShopBalance_Click(object sender, RoutedEventArgs e)
        {
            HideAllTransactionsSubPanels();
            ShopBalancePanel.Visibility = Visibility.Visible;

            double stockValue = inventory.CalculateStockValue();
            int totalStockUnits = inventory.GetTotalStockUnits();

            inventory.GetShopBalanceData(out double revenue, out double totalDiscountApplied, out int totalPhonesSold);

            double totalBalance = stockValue + revenue;

            StockValueText.Text = $"£{stockValue:N2}";
            RevenueText.Text = $"£{revenue:N2}";
            DiscountAppliedText.Text = $"£{totalDiscountApplied:N2}";
            TotalBalanceText.Text = $"£{totalBalance:N2}";

            StockUnitsText.Text = totalStockUnits.ToString();
            PhonesSoldText.Text = totalPhonesSold.ToString();
        }


        // View Order History
        private void ViewOrderHistory_Click(object sender, RoutedEventArgs e)
        {
            HideAllTransactionsSubPanels();
            OrderHistoryPanel.Visibility = Visibility.Visible;

            List<Transaction> history = DatabaseManager.LoadTransactions();

            OrderHistoryGrid.ItemsSource = null;
            OrderHistoryGrid.ItemsSource = history;

            if (history.Count == 0)
            {
                MessageBox.Show("No transactions found.");
            }
        }


        // View Clients
        private void ViewClients_Click(object sender, RoutedEventArgs e)
        {
            HideAllTransactionsSubPanels();
            ClientsPanel.Visibility = Visibility.Visible;

            List<Client> clientList = DatabaseManager.LoadClients();

            ClientsGrid.ItemsSource = null;
            ClientsGrid.ItemsSource = clientList;

            if (clientList.Count == 0)
            {
                MessageBox.Show("No clients found.");
            }
        }


        // View Inventory Log 
        private void ViewInventoryLog_Click(object sender, RoutedEventArgs e)
        {
            HideAllTransactionsSubPanels();
            InventoryLogPanel.Visibility = Visibility.Visible;

            List<InventoryLog> logList = DatabaseManager.LoadInventoryLogs();

            InventoryLogGrid.ItemsSource = null;
            InventoryLogGrid.ItemsSource = logList;

            if (logList.Count == 0)
            {
                MessageBox.Show("No inventory log entries found.");
            }
        }


        private void BackFromTransactions_Click(object sender, RoutedEventArgs e)
        {
            TransactionsPanel.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Visible;
        }



        // HELPER METHODS

        private void UpdatePaymentSummary()
        {
            double basePrice = selectedOrderBasePrice;
            double discountPercent = 0.0;
            double discountAmount = 0.0;
            double totalPayable = basePrice;

            MonthlyInstallmentsLabel.Visibility = Visibility.Collapsed;
            MonthlyInstallmentsText.Visibility = Visibility.Collapsed;
            MonthlyInstallmentsText.Text = "£0.00";

            if (ApplyDiscountBox.SelectedItem is ComboBoxItem discountItem)
            {
                string applyDiscount = discountItem.Content?.ToString() ?? "";

                if (applyDiscount == "Yes" && discountValidated)
                {
                    discountPercent = validatedDiscountPercent;
                    discountAmount = basePrice * (discountPercent / 100.0);
                    totalPayable = basePrice - discountAmount;
                }
            }

            BasePriceText.Text = $"Base Price: £{basePrice:F2}";
            DiscountAmountText.Text = $"Discount Amount: £{discountAmount:F2}";
            TotalPayableText.Text = $"Total Payable: £{totalPayable:F2}";

            if (PaymentMethodBox.SelectedItem is ComboBoxItem paymentMethodItem &&
                PaymentOptionBox.SelectedItem is ComboBoxItem paymentOptionItem &&
                PendingOrdersGrid.SelectedItem is PendingOrderDisplay selectedOrder)
            {
                string paymentMethod = paymentMethodItem.Content?.ToString() ?? "";
                string paymentOption = paymentOptionItem.Content?.ToString() ?? "";

                if (paymentMethod == "CARD" && paymentOption == "Monthly Instalments")
                {
                    var contract = selectedOrder.OrderRef.GetContract();
                    int months = 0;

                    if (contract is PhoneSimPackage package)
                    {
                        months = package.Months;
                    }
                    else if (contract is HireContract hire)
                    {
                        months = hire.Years * 12;
                    }

                    if (months > 0)
                    {
                        double monthlyInstallment = totalPayable / months;

                        MonthlyInstallmentsLabel.Visibility = Visibility.Visible;
                        MonthlyInstallmentsText.Visibility = Visibility.Visible;
                        MonthlyInstallmentsText.Text = $"£{monthlyInstallment:F2} for {months} months";
                    }
                }
            }
        }


        private void UpdateOrderSummary()
        {
            try
            {
                if (CreateOrderPhonesGrid.SelectedItem == null)
                {
                    PhoneDetailsText.Text = "-";
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
                PhoneDetailsText.Text = $"{phone.Manufacturer} {phone.Model} {phone.Storage}GB - £{phone.Price:F2}";


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



        // ACTIONS 

        private void OrderInputChanged(object sender, RoutedEventArgs e)
        {
            UpdateOrderSummary();
        }

        private void TriggerButtonOnEnter(System.Windows.Input.KeyEventArgs e, RoutedEventHandler action)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                action.Invoke(this, new RoutedEventArgs());
        }
    }
}