using System.Threading;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.Maui.Storage;

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
    }
}