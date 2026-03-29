using PhoneMaster.Core.Models;
using System.Windows;
using System.Windows.Input;

namespace PhoneMaster.GUI
{
    public partial class AuthWindow : Window
    {
        public Staff? LoggedUser { get; private set; }

        public AuthWindow()
        {
            InitializeComponent();
            Loaded += AuthWindow_Loaded;
            KeyDown += AuthWindow_KeyDown;
        }

        private void AuthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameBox.Focus();
        }

        private void AuthWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(sender, new RoutedEventArgs());
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Enter username.");
                UsernameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter password.");
                PasswordBox.Focus();
                return;
            }

            var staff = Staff.LoginFromFile(username, password);

            if (staff == null)
            {
                MessageBox.Show("Invalid credentials.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            LoggedUser = staff;
            DialogResult = true;
            Close();
        }
    }
}