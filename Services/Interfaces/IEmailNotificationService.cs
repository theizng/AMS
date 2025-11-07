using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendMaintenanceStatusChangedAsync(string tenantEmail, MaintenanceRequest request, CancellationToken ct = default);
        Task SendInvoiceAsync(string tenantEmail, string subject, string body, CancellationToken ct = default);
    }
}