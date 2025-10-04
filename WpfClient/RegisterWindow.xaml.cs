// WpfClient/RegisterWindow.xaml.cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WpfClient.Services;

namespace WpfClient
{
    public partial class RegisterWindow : Window
    {
        public string? NewUserName { get; private set; }

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            Msg.Text = "";
            var user = (UserBox.Text ?? "").Trim();
            var email = (EmailBox.Text ?? "").Trim();
            var pass = PassBox.Password ?? "";
            var confirm = ConfirmBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                Msg.Text = "Usuario y contraseña son obligatorios.";
                return;
            }
            if (pass != confirm)
            {
                Msg.Text = "Las contraseñas no coinciden.";
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
                email = user.Contains("@") ? user : $"{user}@local.test";

            try
            {
                await ApiClient.EnsureInitializedAsync();

                // DTO que espera el backend /api/auth/register
                var payload = new { UserName = user, Email = email, Password = pass, ConfirmPassword = confirm };

                var res = await ApiClient.PostJsonAsync("/auth/register", payload);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                    res = await ApiClient.PostJsonAsync("/api/auth/register", payload);

                var text = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = $"[{(int)res.StatusCode}] {text}";
                    return;
                }

                NewUserName = user;
                MessageBox.Show("Usuario creado con éxito. Ya puedes iniciar sesión.", "Eaton MCB",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Msg.Text = ex.Message;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
