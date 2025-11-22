using System.Net.Http.Json;
using System.Text.Json;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    public class GoogleScriptMeterWriter : IOnlineMeterSheetWriter
    {
        private readonly HttpClient _http;
        private readonly string _scriptUrl;
        private readonly string _token;

        public GoogleScriptMeterWriter(HttpClient http, string scriptUrl, string token)
        {
            _http = http;
            _scriptUrl = scriptUrl;
            _token = token;
        }

        public async Task UpdateRowAsync(MeterRow row, CancellationToken ct = default)
        {
            var body = new
            {
                token = _token,
                action = "meterUpdate", // will be lowercased server-side
                roomCode = row.RoomCode,
                previousElectric = row.PreviousElectric,
                currentElectric = row.CurrentElectric,
                previousWater = row.PreviousWater,
                currentWater = row.CurrentWater
            };
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, body, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}: {raw}");

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okProp) && okProp.ValueKind == JsonValueKind.True)
                    return;
                var err = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "Unknown";
                throw new InvalidOperationException(err ?? "Unknown");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Bad JSON response: " + raw);
            }
        }

        public async Task RollForwardAsync(CancellationToken ct = default)
        {
            var body = new { token = _token, action = "meterRollForward" };
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, body, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}: {raw}");

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var okProp) && okProp.ValueKind == JsonValueKind.True)
                    return;
                var err = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "Unknown";
                throw new InvalidOperationException(err ?? "Unknown");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Bad JSON response: " + raw);
            }
        }
    }
}