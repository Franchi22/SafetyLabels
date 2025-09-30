using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfClient
{
    public partial class MainWindow : Window
    {
        // ⚠ Ajusta ESTE valor al puerto real de tu API (mira Api/Properties/launchSettings.json).
        private static readonly string BaseUrl = "https://localhost:7267";

        private static string? Token;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            Msg.Text = "";
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
                var payload = new { user = UserBox.Text, pass = PassBox.Password };
                var res = await http.PostAsync("/auth/login",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = "Credenciales inválidas o API no disponible.";
                    return;
                }

                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                Token = doc.RootElement.GetProperty("token").GetString();

                MessageBox.Show("Bienvenido a Eaton MCB", "Acceso concedido",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: abrir Dashboard
            }
            catch (HttpRequestException ex)
            {
                Msg.Text = $"No se pudo conectar a la API ({BaseUrl}). Revisa que esté ejecutándose.\n{ex.Message}";
            }
            catch (Exception ex)
            {
                Msg.Text = ex.Message;
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var win = new RegisterWindow(BaseUrl) { Owner = this };
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
                using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
                var payload = new { userOrEmail = value.Trim() };
                var res = await http.PostAsync("/auth/forgot",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                var text = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = $"No se pudo procesar la solicitud: {text}";
                    return;
                }

                MessageBox.Show("Si los datos existen, se enviaron instrucciones o una contraseña temporal.",
                    "Solicitud recibida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                Msg.Text = $"No se pudo conectar a la API ({BaseUrl}). Verifica que esté ejecutándose.\n{ex.Message}";
            }
            catch (Exception ex)
            {
                Msg.Text = ex.Message;
            }
        }

        // Diálogo simple para pedir un valor
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

        // Ejemplo para futuras llamadas autenticadas
        private static async Task<string> GetAsync(string url)
        {
            using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var r = await http.GetAsync(url);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadAsStringAsync();
        }
    }
}

