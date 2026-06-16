using System.Windows;

namespace WpfApp3
{
    public partial class UserWindow : Window
    {
        private User currentUser;

        public UserWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            txtWelcome.Text = $"Добро пожаловать, {user.FullName}!";
            txtUserInfo.Text = $"Роль: {user.Role}\nЛогин: {user.Login}";
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}