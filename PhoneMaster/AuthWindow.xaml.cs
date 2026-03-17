using PhoneMaster.Core.Models;
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

namespace PhoneMaster.GUI
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public Staff? LoggedUser { get; private set; }
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var staff = Staff.LoginFromFile(
                UsernameBox.Text,
                PasswordBox.Password
            );

            if (staff == null)
            {
                MessageBox.Show("Invalid credentials.");
                return;
            }

            LoggedUser = staff;
            DialogResult = true;
            Close();
        }
    }
}
