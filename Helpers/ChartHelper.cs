using System.Collections.Generic;
using System.Linq;
using AMS.Models;
using Microcharts;
using SkiaSharp; // for colors

namespace AMS.Helpers
{
    public static class ChartHelper
    {
        public static IEnumerable<ChartEntry> ToRevenueEntries(this IEnumerable<MonthlyValue> source)
        {
            var color = SKColor.Parse("#42A5F5");
            return source.Select(m => new ChartEntry((float)m.Revenue)
            {
                Label = m.MonthLabel,
                ValueLabel = m.Revenue.ToString("N0"),
                Color = color
            });
        }

        public static IEnumerable<ChartEntry> ToProfitEntries(this IEnumerable<MonthlyValue> source)
        {
            var color = SKColor.Parse("#66BB6A");
            return source.Select(m => new ChartEntry((float)m.Profit)
            {
                Label = m.MonthLabel,
                ValueLabel = m.Profit.ToString("N0"),
                Color = color
            });
        }

        public static IEnumerable<ChartEntry> ToUtilitiesElectricEntries(this IEnumerable<MonthlyValue> source)
        {
            var color = SKColor.Parse("#42A5F5");
            return source.Select(m => new ChartEntry((float)m.Utilities1)
            {
                Label = m.MonthLabel,
                ValueLabel = m.Utilities1.ToString("N0"),
                Color = color
            });
        }

        public static IEnumerable<ChartEntry> ToUtilitiesWaterEntries(this IEnumerable<MonthlyValue> source)
        {
            var color = SKColor.Parse("#26C6DA");
            return source.Select(m => new ChartEntry((float)m.Utilities2)
            {
                Label = m.MonthLabel,
                ValueLabel = m.Utilities2.ToString("N0"),
                Color = color
            });
        }

        public static IEnumerable<ChartEntry> BuildPaidUnpaidEntries(int paid, int unpaid)
        {
            var entries = new List<ChartEntry>();
            if (paid > 0)
            {
                entries.Add(new ChartEntry(paid)
                {
                    Label = "Đã trả",
                    ValueLabel = paid.ToString(),
                    Color = SKColor.Parse("#66BB6A")
                });
            }
            if (unpaid > 0)
            {
                entries.Add(new ChartEntry(unpaid)
                {
                    Label = "Chưa trả",
                    ValueLabel = unpaid.ToString(),
                    Color = SKColor.Parse("#EF5350")
                });
            }
            return entries;
        }

        // New: interleave Doanh thu and Lợi nhuận for a "double bar" per month
        public static IEnumerable<ChartEntry> BuildRevenueProfitPairedEntries(IEnumerable<MonthlyValue> source)
        {
            var revColor = SKColor.Parse("#42A5F5");
            var profColor = SKColor.Parse("#66BB6A");

            foreach (var m in source.OrderBy(s => s.Month))
            {
                yield return new ChartEntry((float)m.Revenue)
                {
                    Label = m.MonthLabel, // show month label under the first bar
                    ValueLabel = m.Revenue.ToString("N0"),
                    Color = revColor
                };
                yield return new ChartEntry((float)m.Profit)
                {
                    Label = "", // keep axis clean by hiding duplicate month label
                    ValueLabel = m.Profit.ToString("N0"),
                    Color = profColor
                };
            }
        }
    }
}