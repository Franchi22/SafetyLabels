using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WpfClient
{
    public partial class RegisterWindow : Window
    {
        private readonly string _httpsUrl;
        private readonly string _httpUrl;
        public string? NewUserName { get; private set; }

        // ⬅️ NUEVO: constructor que acepta SOLO un URL (el que ya usas: BaseUrl)
        public RegisterWindow(string baseUrl) : this(
            // si viene https usamos ese y generamos el http
            baseUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? baseUrl : baseUrl.Replace("http://", "https://"),
            // y este es el http “pareja”; si venía http, lo dejamos
            baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? baseUrl : baseUrl.Replace("https://", "http://"))
        { }

        // Constructor “completo” (el que ya teníamos)
        public RegisterWindow(string httpsUrl, string httpUrl)
        {
            InitializeComponent();
            _httpsUrl = httpsUrl;
            _httpUrl = httpUrl;
        }

        private static HttpClient CreateClient(string baseUrl)
        {
#if DEBUG
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            return new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
#else
            return new HttpClient { BaseAddress = new Uri(baseUrl) };
#endif
        }

        private static async Task<HttpResponseMessage> PostSmartAsync(string httpsUrl, string httpUrl, string path, string json)
        {
            try
            {
                using var https = CreateClient(httpsUrl);
                return await https.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
            }
            catch (HttpRequestException)
            {
                using var http = CreateClient(httpUrl);
                return await http.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
            }
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
                var payload = JsonSerializer.Serialize(new { user, email, pass });
                var res = await PostSmartAsync(_httpsUrl, _httpUrl, "/auth/register", payload);
                var text = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    Msg.Text = $"No se pudo crear el usuario: {text}";
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
