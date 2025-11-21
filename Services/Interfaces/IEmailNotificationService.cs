using System.Threading;
using System.Threading.Tasks;
using AMS.Models;

namespace AMS.Services.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendMaintenanceStatusChangedAsync(MaintenanceRequest request, CancellationToken ct = default);
        Task SendInvoiceAsync(string tenantEmail, string subject, string body, CancellationToken ct = default);
        Task SendInvoicePdfAsync(RoomTenantInfo roomInfo, string roomCode, int year, int month, PaymentSettings settings, string pdfPath, CancellationToken ct = default);
        Task SendContractDraftAsync(Contract contract, CancellationToken ct = default);
        Task SendContractPdfAsync(Contract contract, CancellationToken ct = default);
        Task SendContractTerminatedAsync(Contract contract, CancellationToken ct = default);
        Task SendContractActivatedAsync(Contract contract, CancellationToken ct = default);
        Task SendContractAddendumNeededAsync(Contract contract, CancellationToken ct = default);
        Task SendToAllTenantsAsync(Contract contract, string subject, string body, string? attachmentName = null, byte[]? attachmentBytes = null, CancellationToken ct = default);
        String BuildContractBody(Contract contract, string heading, string note);
        Task SendContractAddendumAsync(Contract parent, ContractAddendum addendum, CancellationToken ct = default);
        Task SendContractPdfFromPathAsync(Contract contract, string pdfPath, CancellationToken ct = default);
        Task SendPasswordResetAsync(string toEmail, string adminName, string tempPassword);
    }
}