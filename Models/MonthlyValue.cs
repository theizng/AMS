namespace AMS.Models
{
    // For bar charts by month
    public class MonthlyValue
    {
        public int Month { get; set; }          // 1..12
        public decimal Revenue { get; set; }    // or generic Value depending on use
        public decimal Profit { get; set; }     // for overview double bar
        public decimal Utilities1 { get; set; } // e.g., Electric
        public decimal Utilities2 { get; set; } // e.g., Water

        public string MonthLabel => $"{Month:00}";
    }
}