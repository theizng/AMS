using AMS.Models;
using AMS.Services.Interfaces;
using System.Net.Http.Headers;

namespace AMS.Services
{
    public class GoogleSheetXlsxMaintenanceReader : IOnlineMaintenanceReader
    {
        private readonly IMaintenanceSheetReader _xlsxReader;

        public GoogleSheetXlsxMaintenanceReader(IMaintenanceSheetReader xlsxReader)
        {
            _xlsxReader = xlsxReader;
        }

        public async Task<IReadOnlyList<MaintenanceRequest>> ReadFromUrlAsync(string sheetUrl, string? sheetName = null, CancellationToken ct = default)
        {
            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(sheetUrl, out var exportUrl))
                throw new InvalidOperationException("Liên kết Google Sheet không hợp lệ. Hãy dán URL dạng 'docs.google.com/spreadsheets/d/...'.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

            using var resp = await http.GetAsync(exportUrl, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var why = resp.StatusCode == System.Net.HttpStatusCode.Forbidden ? "Sheet có thể không được chia sẻ công khai hoặc không 'Publish to web'." : $"HTTP {(int)resp.StatusCode}";
                throw new InvalidOperationException($"Không thể tải Google Sheet (xlsx export). {why}");
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"maint_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

            try
            {
                await File.WriteAllBytesAsync(tempPath, bytes, ct);
                // Reuse your ClosedXML reader and schema mapping (Vietnamese headers supported)
                var list = await _xlsxReader.ReadAsync(tempPath, sheetName);
                return list;
            }
            finally
            {
                // Clean up temp
                try { File.Delete(tempPath); } catch { /* ignore */ }
            }
        }
    }
}