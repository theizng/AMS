namespace AMS.Models
{
    // For bar charts by month
    public class MonthlyValue
    {
        public int Month { get; set; }          // 1..12
        public decimal Revenue { get; set; }    // reused in revenue charts
        public decimal Profit { get; set; }     // reused in profit charts
        public decimal Utilities1 { get; set; } // Electric
        public decimal Utilities2 { get; set; } // Water
        public decimal GeneralFees { get; set; } // Sum of FeeInstance amounts (phí chung)

        public string MonthLabel => $"{Month:00}";
    }
}