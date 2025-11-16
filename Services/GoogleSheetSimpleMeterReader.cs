using AMS.Services.Interfaces;
using System.Net.Http.Headers;

namespace AMS.Services
{
    public class GoogleSheetSimpleMeterReader : IOnlineMeterSheetReader
    {
        private readonly IMeterSheetReader _localReader;

        public GoogleSheetSimpleMeterReader(IMeterSheetReader localReader)
        {
            _localReader = localReader;
        }

        public async Task<IReadOnlyList<MeterRow>> ReadFromUrlAsync(string sheetUrl)
        {
            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(sheetUrl, out var exportUrl))
                throw new InvalidOperationException("URL Google Sheet không hợp lệ.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

            using var resp = await http.GetAsync(exportUrl);
            if (!resp.IsSuccessStatusCode)
            {
                var why = resp.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Sheet chưa bật chia sẻ hoặc không publish."
                    : $"HTTP {(int)resp.StatusCode}";
                throw new InvalidOperationException("Không thể tải sheet: " + why);
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"meter_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

            try
            {
                await File.WriteAllBytesAsync(tempPath, bytes);
                return await _localReader.ReadAsync(tempPath);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { /* ignore */ }
            }
        }
    }
}