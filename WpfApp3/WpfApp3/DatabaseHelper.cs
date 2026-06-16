using System;
using System.Data;
using System.Data.SqlClient;

namespace WpfApp3
{
    public class DatabaseHelper
    {
        private const int MaxFailedAttempts = 3;
        private const string AdminRole = "Администратор";
        private readonly string serverConnectionString = @"Server=localhost;Trusted_Connection=True;";
        private readonly string connectionString = @"Server=localhost;Database=TEST;Trusted_Connection=True;";

        public DatabaseHelper()
        {
            EnsureDatabase();
            EnsureUsersTable();
        }

        public User AuthenticateUser(string login, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Users WHERE Login = @Login AND Password = @Password";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Login", login);
                command.Parameters.AddWithValue("@Password", password);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                return reader.Read() ? MapUser(reader) : null;
            }
        }

        public User GetUserByLogin(string login)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Users WHERE Login = @Login";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Login", login);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                return reader.Read() ? MapUser(reader) : null;
            }
        }

        public User GetUserById(int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Users WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                return reader.Read() ? MapUser(reader) : null;
            }
        }

        public LoginAttemptResult RegisterFailedAttempt(string login)
        {
            User user = GetUserByLogin(login);
            if (user == null)
                return new LoginAttemptResult { UserExists = false };

            if (user.Role == AdminRole)
            {
                UnblockUser(user.Id);
                return new LoginAttemptResult
                {
                    UserExists = true,
                    FailedAttempts = 0,
                    IsBlocked = false
                };
            }

            int attempts = user.FailedAttempts + 1;
            bool isBlocked = attempts >= MaxFailedAttempts;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Users SET FailedAttempts = @Attempts, IsBlocked = @IsBlocked WHERE Login = @Login";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Attempts", attempts);
                command.Parameters.AddWithValue("@IsBlocked", isBlocked);
                command.Parameters.AddWithValue("@Login", login);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return new LoginAttemptResult
            {
                UserExists = true,
                FailedAttempts = attempts,
                IsBlocked = isBlocked
            };
        }

        public void ResetFailedAttempts(string login)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Users SET FailedAttempts = 0 WHERE Login = @Login";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Login", login);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public DataTable GetAllUsers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Login, FullName, Role, IsBlocked, CreatedDate FROM Users";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public bool AddUser(User user)
        {
            if (GetUserByLogin(user.Login) != null)
                return false;

            if (user.Role == AdminRole)
                user.IsBlocked = false;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"INSERT INTO Users (Login, Password, FullName, Role, IsBlocked)
                                 VALUES (@Login, @Password, @FullName, @Role, @IsBlocked)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@IsBlocked", user.IsBlocked);

                connection.Open();
                command.ExecuteNonQuery();
                return true;
            }
        }

        public void UpdateUser(User user)
        {
            if (user.Role == AdminRole)
            {
                user.IsBlocked = false;
                user.FailedAttempts = 0;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"UPDATE Users
                                 SET Login = @Login,
                                     Password = @Password,
                                     FullName = @FullName,
                                     Role = @Role,
                                     IsBlocked = @IsBlocked,
                                     FailedAttempts = @FailedAttempts
                                 WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", user.Id);
                command.Parameters.AddWithValue("@Login", user.Login);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@IsBlocked", user.IsBlocked);
                command.Parameters.AddWithValue("@FailedAttempts", user.FailedAttempts);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public bool IsLoginBusy(string login, int ignoredUserId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Users WHERE Login = @Login AND Id <> @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Login", login);
                command.Parameters.AddWithValue("@Id", ignoredUserId);

                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public void UnblockUser(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Users SET IsBlocked = 0, FailedAttempts = 0 WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", userId);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void EnsureDatabase()
        {
            using (SqlConnection connection = new SqlConnection(serverConnectionString))
            {
                string query = "IF DB_ID(N'TEST') IS NULL CREATE DATABASE TEST";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void EnsureUsersTable()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"
IF OBJECT_ID(N'Users', N'U') IS NULL
BEGIN
    CREATE TABLE Users
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Login NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Role NVARCHAR(30) NOT NULL,
        IsBlocked BIT NOT NULL DEFAULT 0,
        FailedAttempts INT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO Users (Login, Password, FullName, Role)
    VALUES
        (N'admin', N'admin', N'Администратор', N'Администратор'),
        (N'user', N'user', N'Пользователь', N'Пользователь');
END

UPDATE Users
SET FullName = N'Администратор',
    Role = N'Администратор',
    IsBlocked = 0,
    FailedAttempts = 0
WHERE Login = N'admin';";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Login = reader["Login"].ToString(),
                Password = reader["Password"].ToString(),
                FullName = reader["FullName"].ToString(),
                Role = reader["Role"].ToString(),
                IsBlocked = Convert.ToBoolean(reader["IsBlocked"]),
                FailedAttempts = Convert.ToInt32(reader["FailedAttempts"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }

    public class LoginAttemptResult
    {
        public bool UserExists { get; set; }
        public int FailedAttempts { get; set; }
        public bool IsBlocked { get; set; }
    }
}
