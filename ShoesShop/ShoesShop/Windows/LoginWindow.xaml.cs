using ShoesShop.Model;
using System.Linq;
using System.Windows;

namespace ShoesShop.Windows
{
    public partial class LoginWindow : Window
    {
        DemShoesShopEntities context = new DemShoesShopEntities();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            Users user = context.Users
                .FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close();
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(null);
            mainWindow.Show();
            this.Close();
        }
    }
}