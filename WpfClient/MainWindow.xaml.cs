// WpfClient/MainWindow.xaml.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfClient.Services;

namespace WpfClient
{
    public partial class MainWindow : Window
    {
        private static string? Token;

        public MainWindow() { InitializeComponent(); }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            Msg.Text = "";
            var user = (UserBox.Text ?? "").Trim();
            var pass = PassBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                Msg.Text = "Usuario y contraseña son obligatorios.";
                return;
            }

            try
            {
                await ApiClient.EnsureInitializedAsync();

                var payload = new { UserName = user, Password = pass };

                var res = await ApiClient.PostJsonAsync("/auth/login", payload);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                    res = await ApiClient.PostJsonAsync("/api/auth/login", payload);

                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = $"[{(int)res.StatusCode}] {body}";
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                Token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;

                if (string.IsNullOrWhiteSpace(Token))
                {
                    Msg.Text = "Inicio correcto, pero no se recibió el token.";
                    return;
                }

                MessageBox.Show("Bienvenido a Eaton MCB", "Acceso concedido",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                Msg.Text = $"No se pudo conectar a la API ({ApiClient.BaseUrl}). {ex.Message}";
            }
            catch (Exception ex)
            {
                Msg.Text = ex.Message;
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var win = new RegisterWindow { Owner = this };
            var ok = win.ShowDialog();
            if (ok == true && !string.IsNullOrWhiteSpace(win.NewUserName))
            {
                UserBox.Text = win.NewUserName;
                PassBox.Focus();
            }
        }

        private async void Forgot_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Msg.Text = "";
            var (ok, value) = Prompt("Recuperación de credenciales", "Escribe tu usuario o correo:");
            if (!ok || string.IsNullOrWhiteSpace(value)) return;

            try
            {
                await ApiClient.EnsureInitializedAsync();

                var payload = new { UserOrEmail = value.Trim() };

                var res = await ApiClient.PostJsonAsync("/auth/forgot", payload);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                    res = await ApiClient.PostJsonAsync("/api/auth/forgot", payload);

                var text = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = $"[{(int)res.StatusCode}] {text}";
                    return;
                }

                MessageBox.Show(
                    "Si los datos existen, se enviaron instrucciones o una contraseña temporal.",
                    "Solicitud recibida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                Msg.Text = $"No se pudo conectar a la API ({ApiClient.BaseUrl}). {ex.Message}";
            }
            catch (Exception ex)
            {
                Msg.Text = ex.Message;
            }
        }

        private static (bool ok, string value) Prompt(string title, string message)
        {
            var win = new Window
            {
                Title = title,
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };
            var panel = new StackPanel { Margin = new Thickness(14) };
            panel.Children.Add(new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) });
            var tb = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
            panel.Children.Add(tb);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "Enviar", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            var cancelBtn = new Button { Content = "Cancelar", Width = 90 };
            bool ok = false;
            okBtn.Click += (_, __) => { ok = true; win.Close(); };
            cancelBtn.Click += (_, __) => { win.Close(); };
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            panel.Children.Add(buttons);

            win.Content = panel;
            win.ShowDialog();
            return (ok, tb.Text ?? "");
        }

        private static async Task<string> GetAuthAsync(string path)
        {
            await ApiClient.EnsureInitializedAsync();
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrWhiteSpace(Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            var res = await ApiClient.Http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }
    }
}
