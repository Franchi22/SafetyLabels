// WpfClient/Services/ApiClient.cs
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WpfClient.Services
{
    public static class ApiClient
    {
        public static string BaseUrl { get; private set; } = "https://localhost:7267";
        public static HttpClient Http { get; private set; } = CreateClient(BaseUrl);

        private static bool _initialized;
        private static readonly string[] CandidateBaseUrls = new[]
        {
            "https://localhost:7267",
            "http://localhost:5267"
        };

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

        public static async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            Exception? last = null;
            foreach (var url in CandidateBaseUrls)
            {
                try
                {
                    using var probe = CreateClient(url);
                    var r = await probe.GetAsync("/health");
                    if (r.IsSuccessStatusCode)
                    {
                        BaseUrl = url;
                        Http.Dispose();
                        Http = CreateClient(BaseUrl);
                        _initialized = true;
                        return;
                    }
                }
                catch (Exception ex) { last = ex; }
            }

            throw new HttpRequestException(
                $"No pude conectar con la API en 7267 (https) ni 5267 (http).",
                last);
        }

        public static Task<HttpResponseMessage> GetAsync(string path)
            => Http.GetAsync(path);

        public static Task<HttpResponseMessage> PostJsonAsync(string path, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return Http.PostAsync(path, content);
        }
    }
}
