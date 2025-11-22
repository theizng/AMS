using System.Threading;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace AMS.Services
{
    public class PaymentSettingsProvider : IPaymentSettingsProvider
    {
        private const string K_NameAccount = "pay:nameAccount";
        private const string K_BankAccount = "pay:bankAccount";
        private const string K_BankName = "pay:bankName";
        private const string K_Branch = "pay:branch";
        private const string K_DueDay = "pay:dueDay";
        private const string K_GraceDays = "pay:graceDays";
        private const string K_ElecRate = "pay:def:elecRate";
        private const string K_WaterRate = "pay:def:waterRate";
        private const string K_LateFee = "pay:def:lateFee";

        public PaymentSettings Get()
        {
            var s = new PaymentSettings
            {
                NameAccount = Preferences.Get(K_NameAccount, ""),
                BankAccount = Preferences.Get(K_BankAccount, ""),
                BankName = Preferences.Get(K_BankName, ""),
                Branch = Preferences.Get(K_Branch, ""),
                DefaultDueDay = Preferences.Get(K_DueDay, 5),
                GraceDays = Preferences.Get(K_GraceDays, 0),
                DefaultElectricRate = decimal.TryParse(Preferences.Get(K_ElecRate, "0"), out var er) ? er : 0,
                DefaultWaterRate = decimal.TryParse(Preferences.Get(K_WaterRate, "0"), out var wr) ? wr : 0,
                LateFeeRate = decimal.TryParse(Preferences.Get(K_LateFee, "50000"), out var lf) ? lf : 50000m
            };
            return s;
        }

        public Task SaveAsync(PaymentSettings s, CancellationToken ct = default)
        {
            Preferences.Set(K_NameAccount, s.NameAccount ?? "");
            Preferences.Set(K_BankAccount, s.BankAccount ?? "");
            Preferences.Set(K_BankName, s.BankName ?? "");
            Preferences.Set(K_Branch, s.Branch ?? "");
            Preferences.Set(K_DueDay, s.DefaultDueDay);
            Preferences.Set(K_GraceDays, s.GraceDays);
            Preferences.Set(K_ElecRate, s.DefaultElectricRate.ToString());
            Preferences.Set(K_WaterRate, s.DefaultWaterRate.ToString());
            Preferences.Set(K_LateFee, s.LateFeeRate.ToString());
            return Task.CompletedTask;
        }
    }
}