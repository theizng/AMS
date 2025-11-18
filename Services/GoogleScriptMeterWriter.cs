using System.Net.Http.Json;
using System.Text.Json;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    // Encapsulates ScriptUrl + Token internally; caller never passes them again.
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
                action = "meterUpdate",
                roomCode = row.RoomCode,
                currentElectric = row.CurrentElectric,
                currentWater = row.CurrentWater
            };
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, body, ct);
            resp.EnsureSuccessStatusCode();
            var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.GetProperty("ok").GetBoolean())
                throw new InvalidOperationException(doc.RootElement.GetProperty("error").GetString());
        }

        public async Task RollForwardAsync(CancellationToken ct = default)
        {
            var body = new { token = _token, action = "meterRollForward" };
            using var resp = await _http.PostAsJsonAsync(_scriptUrl, body, ct);
            resp.EnsureSuccessStatusCode();
            var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.GetProperty("ok").GetBoolean())
                throw new InvalidOperationException(doc.RootElement.GetProperty("error").GetString());
        }
    }
}