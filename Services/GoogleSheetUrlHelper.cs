using System.Text.RegularExpressions;

namespace AMS.Services
{
    public static class GoogleSheetUrlHelper
    {
        public static bool TryBuildExportXlsxUrl(string input, out string exportUrl)
        {
            exportUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)) return false;

            // If already export/published xlsx, ensure format=xlsx present
            var isExportPath = uri.AbsolutePath.Contains("/export", StringComparison.OrdinalIgnoreCase);
            var query = uri.Query ?? string.Empty;
            var hasFormatXlsx = query.Contains("format=xlsx", StringComparison.OrdinalIgnoreCase)
                                || query.Contains("output=xlsx", StringComparison.OrdinalIgnoreCase);

            if (isExportPath && hasFormatXlsx)
            {
                // add cache buster
                exportUrl = uri.ToString() + (string.IsNullOrEmpty(uri.Query) ? "?" : "&") + $"r={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                return true;
            }

            // Extract spreadsheet id
            var m = Regex.Match(uri.AbsolutePath, @"/spreadsheets/d/([a-zA-Z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            var id = m.Groups[1].Value;

            // Extract gid from fragment or query
            string? gid = null;
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                var mf = Regex.Match(uri.Fragment, @"gid=(\d+)");
                if (mf.Success) gid = mf.Groups[1].Value;
            }
            if (gid == null)
            {
                var mg = Regex.Match(uri.Query, @"[?&]gid=(\d+)");
                if (mg.Success) gid = mg.Groups[1].Value;
            }

            exportUrl = $"https://docs.google.com/spreadsheets/d/{id}/export?format=xlsx";
            if (!string.IsNullOrEmpty(gid)) exportUrl += $"&gid={gid}";
            exportUrl += $"&r={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            return true;
        }
    }
}