using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using PhoneMaster.Core.Models;

namespace PhoneMaster.GUI
{
    public partial class ClientDetailsWindow : Window
    {
        private readonly string clientType;
        public Client? CreatedClient { get; private set; }

        public ClientDetailsWindow(string clientType)
        {
            InitializeComponent();
            this.clientType = clientType;

            ClientTypeTitle.Text = clientType == "Company"
                ? "Company Details"
                : "Customer Details";

            if (clientType == "Company")
            {
                VatLabel.Visibility = Visibility.Visible;
                VatNumberBox.Visibility = Visibility.Visible;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
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
                return;
            }

            if (clientType == "Company")
            {
                string vatNumber = VatNumberBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(vatNumber))
                {
                    MessageBox.Show("Enter VAT number.");
                    return;
                }

                CreatedClient = new Client(name, vatNumber, email, contactPhone, address, postcode, town);
            }
            else
            {
                CreatedClient = new Client(name, email, contactPhone, address, postcode, town);
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}