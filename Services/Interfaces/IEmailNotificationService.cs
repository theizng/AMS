using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendMaintenanceStatusChangedAsync(string tenantEmail, MaintenanceRequest request, CancellationToken ct = default);
        Task SendInvoiceAsync(string tenantEmail, string subject, string body, CancellationToken ct = default);

        Task SendContractDraftAsync(Contract contract, CancellationToken ct = default);
        Task SendContractPdfAsync(Contract contract, CancellationToken ct = default);
        Task SendContractTerminatedAsync(Contract contract, CancellationToken ct = default);
        Task SendContractActivatedAsync(Contract contract, CancellationToken ct = default);
        Task SendContractAddendumNeededAsync(Contract contract, CancellationToken ct = default);
        Task SendToAllTenants(Contract contract, string subject, string body, CancellationToken ct);
        String BuildContractBody(Contract contract, string heading, string note);

    }
}