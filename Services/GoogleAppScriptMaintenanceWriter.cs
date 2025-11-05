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
        private readonly string? _sheetName;

        public GoogleAppScriptMaintenanceWriter(HttpClient http, string scriptUrl, string token, string? sheetName = null)
        {
            _http = http;
            _scriptUrl = scriptUrl;
            _token = token;
            _sheetName = sheetName;
        }

        public async Task UpdateAsync(string requestId, IDictionary<string, object> values, CancellationToken ct = default)
        {
            var payload = new { token = _token, action = "update", requestId, values, sheetName = _sheetName };
            await PostExpectOk(payload, ct);
        }

        public async Task<string> CreateAsync(IDictionary<string, object> values, CancellationToken ct = default)
        {
            var payload = new { token = _token, action = "create", values, sheetName = _sheetName };
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, payload, ct);
            resp.EnsureSuccessStatusCode();
            var txt = await resp.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(txt);
            if (!doc.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
                throw new InvalidOperationException($"Script failure: {txt}");
            if (doc.RootElement.TryGetProperty("requestId", out var idEl))
                return idEl.GetString() ?? "";
            return "";
        }

        public async Task DeleteAsync(string requestId, CancellationToken ct = default)
        {
            var payload = new { token = _token, action = "delete", requestId, sheetName = _sheetName };
            await PostExpectOk(payload, ct);
        }

        public async Task SyncRoomsAsync(IEnumerable<RoomInfo> rooms, CancellationToken ct = default)
        {
            var payload = new { token = _token, action = "syncRooms", rooms, sheetName = _sheetName };
            await PostExpectOk(payload, ct);
        }

        private async Task PostExpectOk(object payload, CancellationToken ct)
        {
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, payload, ct);
            resp.EnsureSuccessStatusCode();
            var text = await resp.Content.ReadAsStringAsync(ct);
            var trimmed = text.TrimStart();
            if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
                throw new InvalidOperationException($"Script did not return JSON. First chars: {trimmed[..Math.Min(160, trimmed.Length)]}");
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
                throw new InvalidOperationException($"Script returned failure: {text}");
        }
    }
}