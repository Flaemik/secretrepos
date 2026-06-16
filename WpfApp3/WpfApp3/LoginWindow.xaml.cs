using System.Windows;
using System.Windows.Media;

namespace WpfApp3
{
    public partial class LoginWindow : Window
    {
        private DatabaseHelper dbHelper;
        private PuzzleCaptcha puzzleCaptcha;
        private int failedAttempts = 0;
        private const int MaxAttempts = 3;

        public LoginWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            InitializeCaptcha();
        }

        private void InitializeCaptcha()
        {
            puzzleCaptcha = new PuzzleCaptcha(captchaCanvas);
            puzzleCaptcha.CaptchaCompleted += PuzzleCaptcha_CaptchaCompleted;
        }

        private void PuzzleCaptcha_CaptchaCompleted(object sender, bool isCompleted)
        {
            if (isCompleted)
            {
                btnLogin.IsEnabled = true;
            }
        }

        private void BtnResetCaptcha_Click(object sender, RoutedEventArgs e)
        {
            puzzleCaptcha.Reset();
            btnLogin.IsEnabled = true;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Поля 'Логин' и 'Пароль' должны быть заполнены!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingUser = dbHelper.GetUserByLogin(login);
            if (existingUser != null && existingUser.IsBlocked && !IsAdmin(existingUser))
            {
                ShowBlockedMessage();
                return;
            }

            if (!puzzleCaptcha.IsCaptchaCompleted())
            {
                HandleFailedAttempt(login, "Пожалуйста, соберите пазл правильно!",
                    "Ошибка капчи", MessageBoxImage.Warning);
                return;
            }

            var user = dbHelper.AuthenticateUser(login, password);

            if (user == null)
            {
                HandleFailedAttempt(login,
                    "Вы ввели неверный логин или пароль. Пожалуйста проверьте ещё раз введенные данные",
                    "Ошибка авторизации", MessageBoxImage.Error);
            }
            else if (user.IsBlocked && !IsAdmin(user))
            {
                ShowBlockedMessage();
            }
            else
            {
                dbHelper.ResetFailedAttempts(login);
                failedAttempts = 0;
                UpdateAttemptsInfo(0);

                MessageBox.Show("Вы успешно авторизовались",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                if (user.Role == "Администратор")
                {
                    AdminWindow adminWindow = new AdminWindow(user);
                    adminWindow.Show();
                }
                else
                {
                    UserWindow userWindow = new UserWindow(user);
                    userWindow.Show();
                }

                this.Close();
            }
        }

        private void HandleFailedAttempt(string login, string message, string title, MessageBoxImage image)
        {
            LoginAttemptResult result = dbHelper.RegisterFailedAttempt(login);
            if (result.UserExists)
            {
                UpdateAttemptsInfo(result.FailedAttempts);

                if (result.IsBlocked)
                {
                    ShowBlockedMessage();
                    return;
                }
            }
            else
            {
                failedAttempts++;
                UpdateAttemptsInfo(failedAttempts);
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }

        private void ShowBlockedMessage()
        {
            MessageBox.Show("Вы заблокированы. Обратитесь к администратору",
                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        private void UpdateAttemptsInfo(int currentAttempts)
        {
            int remaining = MaxAttempts - currentAttempts;
            txtAttemptsInfo.Text = $"Осталось попыток: {remaining}";
            if (remaining <= 0)
            {
                txtAttemptsInfo.Text = "Попытки исчерпаны";
                txtAttemptsInfo.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtAttemptsInfo.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private bool IsAdmin(User user)
        {
            return user != null && user.Role == "Администратор";
        }
    }
}
