using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.Maui.Storage;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IEmailService _email;

        // Keys must match SettingsViewModel
        private const string K_SmtpHost = "email:smtp:host";
        private const string K_SmtpPort = "email:smtp:port";
        private const string K_SmtpSsl = "email:smtp:ssl";
        private const string K_SmtpUser = "email:smtp:user";
        private const string K_SmtpPwd = "email:smtp:pwd";
        private const string K_SenderName = "email:sender:name";
        private const string K_SenderAddr = "email:sender:addr";

        public EmailNotificationService(IEmailService email) => _email = email;

        public async Task SendMaintenanceStatusChangedAsync(string tenantEmail, MaintenanceRequest req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantEmail)) return;

            var (host, port, ssl, user, pwd, senderName, senderAddr) = await LoadSmtpAsync();

            var subject = $"[AMS] Cập nhật bảo trì - {req.RoomCode} - {req.StatusVi}";
            var body =
$@"Xin chào,

Yêu cầu bảo trì có mã {req.RequestId} cho phòng {req.RoomCode} đã được cập nhật trạng thái: {req.StatusVi}.
Phân loại: {req.Category}
Mô tả: {req.Description}
Chi phí ước tính: {(req.EstimatedCost.HasValue ? req.EstimatedCost.Value.ToString("N0") + " đ" : "(chưa có)")}

Trân trọng,
AMS";

            await _email.SendAsync(tenantEmail, subject, body, host, port, user, pwd, ssl, senderName, senderAddr, ct);
        }
        public async Task SendInvoiceAsync(string tenantEmail, string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantEmail)) return;
            var (host, port, ssl, user, pwd, senderName, senderAddr) = await LoadSmtpAsync();
            await _email.SendAsync(tenantEmail, subject, body, host, port, user, pwd, ssl, senderName, senderAddr, ct);
        }
        private static async Task<(string host, int port, bool ssl, string user, string pwd, string senderName, string senderAddr)> LoadSmtpAsync()
        {
            var host = Preferences.Get(K_SmtpHost, "");
            var port = int.TryParse(Preferences.Get(K_SmtpPort, "587"), out var p) ? p : 587;
            var ssl = Preferences.Get(K_SmtpSsl, true);
            var user = Preferences.Get(K_SmtpUser, "");
            // Prefer SecureStorage for password; fallback to Preferences (if you kept it there)
            var pwd = await SecureStorage.GetAsync(K_SmtpPwd) ?? Preferences.Get(K_SmtpPwd, "");
            var senderName = Preferences.Get(K_SenderName, "AMS");
            var senderAddr = Preferences.Get(K_SenderAddr, user);
            return (host, port, ssl, user, pwd, senderName, senderAddr);
        }
        public async Task SendContractDraftAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenants(contract,
                subject: $"[AMS] Bản nháp hợp đồng phòng {contract.RoomCode}",
                body: BuildContractBody(contract, "BẢN NHÁP HỢP ĐỒNG", "Vui lòng kiểm tra thông tin và phản hồi nếu cần chỉnh sửa."),
                ct);
        }
        public async Task SendContractPdfAsync(Contract contract, CancellationToken ct = default)
        {
            var pdfNote = string.IsNullOrWhiteSpace(contract.PdfUrl) ? "(Chưa có file PDF đính kèm)" : $"Link PDF: {contract.PdfUrl}";
            await SendToAllTenants(contract,
                subject: $"[AMS] Hợp đồng phòng {contract.RoomCode} (PDF đính kèm)",
                body: BuildContractBody(contract, "HỢP ĐỒNG (PDF)", pdfNote + "\nVui lòng đọc và chấp thuận trước khi kích hoạt."),
                ct);
        }
        public async Task SendContractActivatedAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenants(contract,
                subject: $"[AMS] Hợp đồng phòng {contract.RoomCode} đã kích hoạt",
                body: BuildContractBody(contract, "HỢP ĐỒNG KÍCH HOẠT", "Hợp đồng đã có hiệu lực. Cảm ơn bạn."),
                ct);
        }
        public async Task SendContractTerminatedAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenants(contract,
                subject: $"[AMS] Hợp đồng phòng {contract.RoomCode} đã chấm dứt",
                body: BuildContractBody(contract, "HỢP ĐỒNG CHẤM DỨT", "Vui lòng phối hợp bàn giao phòng / tài sản."),
                ct);
        }
        public async Task SendContractAddendumNeededAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenants(contract,
                subject: $"[AMS] Phòng {contract.RoomCode} cần phụ lục hợp đồng",
                body: BuildContractBody(contract, "PHỤ LỤC HỢP ĐỒNG", "Có thay đổi người thuê. Phụ lục cần được lập để cập nhật thông tin."),
                ct);
        }
        public async Task SendToAllTenants(Contract contract, string subject, string body, CancellationToken ct)
        {
            foreach (var t in contract.Tenants)
            {
                if (!string.IsNullOrWhiteSpace(t.Email))
                    await SendInvoiceAsync(t.Email, subject, body, ct); // reuse generic send
            }
        }
        public string BuildContractBody(Contract c, string heading, string note)
        {
            var sb = new StringBuilder();
            sb.AppendLine(heading);
            sb.AppendLine($"Số HĐ: {c.ContractNumber}");
            sb.AppendLine($"Phòng: {c.RoomCode}");
            sb.AppendLine($"Địa chỉ: {c.HouseAddress}");
            sb.AppendLine($"Thời hạn: {c.StartDate:dd/MM/yyyy} - {c.EndDate:dd/MM/yyyy}");
            sb.AppendLine($"Giá thuê: {c.RentAmount:N0} đ / tháng (thu ngày {c.DueDay})");
            sb.AppendLine($"Đặt cọc: {c.SecurityDeposit:N0} đ");
            if (!string.IsNullOrWhiteSpace(c.PdfUrl))
                sb.AppendLine($"PDF: {c.PdfUrl}");
            sb.AppendLine();
            sb.AppendLine("Người thuê:");
            foreach (var t in c.Tenants)
                sb.AppendLine($"- {t.Name} / {t.Phone} / {t.Email}");
            sb.AppendLine();
            sb.AppendLine(note);
            sb.AppendLine();
            sb.AppendLine("Trân trọng,\nAMS");
            return sb.ToString();
        }
    }
}