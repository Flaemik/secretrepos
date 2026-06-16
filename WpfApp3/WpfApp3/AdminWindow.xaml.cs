using System.Windows;
using System.Data;

namespace WpfApp3
{
    public partial class AdminWindow : Window
    {
        private DatabaseHelper dbHelper;
        private User currentUser;
        private int? selectedUserId = null;

        public AdminWindow(User user)
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            currentUser = user;
            txtAdminInfo.Text = $"Администратор: {user.FullName}";
            LoadUsers();
        }

        private void LoadUsers()
        {
            DataTable users = dbHelper.GetAllUsers();
            dgUsers.ItemsSource = users.DefaultView;
        }

        private void DgUsers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem != null)
            {
                DataRowView row = dgUsers.SelectedItem as DataRowView;
                if (row != null)
                {
                    selectedUserId = System.Convert.ToInt32(row["Id"]);
                    txtLogin.Text = row["Login"].ToString();
                    txtFullName.Text = row["FullName"].ToString();
                    string selectedRole = row["Role"].ToString();

                    foreach (System.Windows.Controls.ComboBoxItem item in cmbRole.Items)
                    {
                        if (item.Content.ToString() == selectedRole)
                        {
                            cmbRole.SelectedItem = item;
                            break;
                        }
                    }

                    chkBlocked.IsChecked = IsAdminRole(selectedRole) ? false : System.Convert.ToBoolean(row["IsBlocked"]);
                    chkBlocked.IsEnabled = !IsAdminRole(selectedRole);
                    txtPassword.Text = "";
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string role = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Логин и пароль обязательны для заполнения!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User newUser = new User
            {
                Login = login,
                Password = password,
                FullName = fullName,
                Role = role ?? "Пользователь",
                IsBlocked = IsAdminRole(role) ? false : (chkBlocked.IsChecked ?? false)
            };

            bool result = dbHelper.AddUser(newUser);

            if (result)
            {
                MessageBox.Show("Пользователь успешно добавлен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
                LoadUsers();
            }
            else
            {
                MessageBox.Show("Пользователь с указанным логином уже существует!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedUserId.HasValue)
            {
                MessageBox.Show("Выберите пользователя для редактирования!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string role = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            bool isBlocked = IsAdminRole(role) ? false : (chkBlocked.IsChecked ?? false);

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Логин обязателен для заполнения!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dbHelper.IsLoginBusy(login, selectedUserId.Value))
            {
                MessageBox.Show("Пользователь с указанным логином уже существует!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            User existingUser = dbHelper.GetUserById(selectedUserId.Value);
            if (existingUser == null)
            {
                MessageBox.Show("Выбранный пользователь не найден. Обновите список пользователей.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadUsers();
                ClearFields();
                return;
            }

            User user = new User
            {
                Id = selectedUserId.Value,
                Login = login,
                Password = string.IsNullOrEmpty(password) ? existingUser.Password : password,
                FullName = fullName,
                Role = role ?? "Пользователь",
                IsBlocked = isBlocked,
                FailedAttempts = isBlocked ? existingUser.FailedAttempts : 0
            };

            dbHelper.UpdateUser(user);
            MessageBox.Show("Данные пользователя обновлены!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearFields();
            LoadUsers();
        }

        private void BtnUnblock_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedUserId.HasValue)
            {
                MessageBox.Show("Выберите пользователя для разблокировки!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dbHelper.UnblockUser(selectedUserId.Value);
            MessageBox.Show("Пользователь разблокирован!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUsers();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ClearFields()
        {
            txtLogin.Clear();
            txtPassword.Clear();
            txtFullName.Clear();
            cmbRole.SelectedIndex = 1;
            chkBlocked.IsChecked = false;
            chkBlocked.IsEnabled = true;
            selectedUserId = null;
        }

        private void CmbRole_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string role = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            bool isAdmin = IsAdminRole(role);
            chkBlocked.IsEnabled = !isAdmin;

            if (isAdmin)
                chkBlocked.IsChecked = false;
        }

        private bool IsAdminRole(string role)
        {
            return role == "Администратор";
        }
    }
}
