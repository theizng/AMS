using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IPaymentSettingsProvider
    {
        PaymentSettings Get();
        Task SaveAsync(PaymentSettings settings, CancellationToken ct = default);
    }
}