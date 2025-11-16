using System;
using System.Collections.Generic;
using System.Linq;

namespace AMS.Services
{
    public static class MeterSimpleSheetSchema
    {
        // Expected order (position-based)
        // A Mã phòng
        // B Chỉ số điện tháng trước
        // C Chỉ số điện hiện tại
        // D Mức tiêu thụ điện
        // E Chỉ số nước tháng trước
        // F Chỉ số nước hiện tại
        // G Mức tiêu thụ nước

        public static readonly Dictionary<string, int> LogicalPositions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["roomcode"] = 1,
            ["prev_elec"] = 2,
            ["cur_elec"] = 3,
            ["cons_elec"] = 4,
            ["prev_water"] = 5,
            ["cur_water"] = 6,
            ["cons_water"] = 7
        };

        // Header normalization to detect duplicates or alias differences
        public static string Normalize(string? h)
        {
            if (string.IsNullOrWhiteSpace(h)) return "";
            h = h.Trim().ToLowerInvariant();
            h = h.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(h.Length);
            foreach (var c in h)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var s = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
            s = s.Replace('đ', 'd').Replace('Đ', 'd');
            return s;
        }

        public static bool LooksWaterPrevious(string h) =>
            Normalize(h).Contains("nuoc") && Normalize(h).Contains("truoc");

        public static bool LooksWaterCurrent(string h) =>
            Normalize(h).Contains("nuoc") && (Normalize(h).Contains("hien") || Normalize(h).Contains("tai"));

        public static bool LooksElectricPrevious(string h) =>
            Normalize(h).Contains("dien") && Normalize(h).Contains("truoc");

        public static bool LooksElectricCurrent(string h) =>
            Normalize(h).Contains("dien") && (Normalize(h).Contains("hien") || Normalize(h).Contains("tai"));
    }
}