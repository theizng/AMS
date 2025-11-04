using System.Net.Http.Json;
using System.Text.Json;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    public class GoogleAppScriptMaintenanceWriter : IMaintenanceSheetWriter
    {
        private readonly HttpClient _http;
        private readonly string _scriptUrl;
        private readonly string _token;
        private readonly string? _sheetName; // optional

        public GoogleAppScriptMaintenanceWriter(HttpClient http, string scriptUrl, string token, string? sheetName = null)
        {
            _http = http;
            _scriptUrl = scriptUrl;
            _token = token;
            _sheetName = sheetName; // e.g., "Yêu cầu bảo trì"
        }

        public async Task UpdateAsync(string requestId, IDictionary<string, object> values, CancellationToken ct = default)
        {
            var payload = new
            {
                token = _token,
                requestId,
                values,
                sheetName = _sheetName // may be null; GAS falls back to default
            };

            using var resp = await _http.PostAsJsonAsync(_scriptUrl, payload, ct);
            resp.EnsureSuccessStatusCode();

            var text = await resp.Content.ReadAsStringAsync(ct);
            var trimmed = text.TrimStart();

            if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
            {
                var snippet = trimmed.Length > 160 ? trimmed[..160] + "..." : trimmed;
                throw new InvalidOperationException($"Script did not return JSON. First chars: {snippet}");
            }

            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
                throw new InvalidOperationException($"Script returned failure: {text}");
        }
    }
}