using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.Maui.Storage;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
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

        // ========= Helpers =========

        private static async Task<(string host, int port, bool ssl, string user, string pwd, string senderName, string senderAddr)> LoadSmtpAsync()
        {
            var host = Preferences.Get(K_SmtpHost, "");
            var port = int.TryParse(Preferences.Get(K_SmtpPort, "587"), out var p) ? p : 587;
            var ssl = Preferences.Get(K_SmtpSsl, true);
            var user = Preferences.Get(K_SmtpUser, "");
            var pwd = await SecureStorage.GetAsync(K_SmtpPwd) ?? Preferences.Get(K_SmtpPwd, "");
            var senderName = Preferences.Get(K_SenderName, "QLT");
            var senderAddr = Preferences.Get(K_SenderAddr, user);
            return (host, port, ssl, user, pwd, senderName, senderAddr);
        }

        private static (string? fileName, byte[]? bytes) TryLoadAttachment(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return (null, null);

            try
            {
                // Normalize file:// URI to local path
                if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
                    path = uri.LocalPath;

                if (File.Exists(path))
                    return (Path.GetFileName(path), File.ReadAllBytes(path));
            }
            catch
            {
                // ignore, will send without attachment
            }
            return (null, null);
        }



        public string BuildInvoiceBody(string roomCode, string heading, string note)
        {
            var sb = new StringBuilder();
            sb.AppendLine(heading);
            sb.AppendLine($"Ban quản lý gửi hóa đơn phòng ");
            sb.AppendLine();
            sb.AppendLine(note);
            sb.AppendLine();
            sb.AppendLine("Trân trọng,\nQLT - Phần mềm quản lý thuê");
            return sb.ToString();
        }

        public async Task SendInvoicePdfAsync(RoomTenantInfo roomInfo, string roomCode, int year, int month, PaymentSettings settings, string pdfPath, CancellationToken ct = default)
        {
            if (roomInfo == null) return;
            var (fileName, bytes) = TryLoadAttachment(pdfPath);
            if (bytes == null || string.IsNullOrWhiteSpace(fileName)) return;

            var subject = $"[QLT] Thông báo hóa đơn phí phòng {roomCode} tháng {month.ToString("00")} (PDF đính kèm)";


            var body = BuildInvoiceBody(roomCode, subject, "Vui lòng kiểm tra kĩ và thanh toán đúng hẹn");

            foreach (var to in roomInfo.Emails)
            {
                if (string.IsNullOrWhiteSpace(to)) continue;
                await SendAsync(to, subject, body, fileName, bytes, ct);
            }
        }

        public async Task SendInvoiceAsync(string tenantEmail, string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantEmail)) return;
            await SendAsync(tenantEmail, subject, body, ct: ct);
        }

        private async Task SendAsync(string to, string subject, string body, string? attachmentName = null, byte[]? attachmentBytes = null, CancellationToken ct = default)
        {
            var (host, port, ssl, user, pwd, senderName, senderAddr) = await LoadSmtpAsync();

            await _email.SendAsync(
                to: to,
                subject: subject,
                body: body,
                smtpHost: host,
                smtpPort: port,
                smtpUser: user,
                smtpPassword: pwd,
                useSsl: ssl,
                senderName: senderName,
                senderAddress: senderAddr,
                attachmentFileName: attachmentName,
                attachmentBytes: attachmentBytes,
                ct: ct);
        }
        public async Task SendToAllTenantsAsync(Contract contract, string subject, string body, string? attachmentName = null, byte[]? attachmentBytes = null, CancellationToken ct = default)
        {
            foreach (var t in contract.Tenants)
            {
                if (string.IsNullOrWhiteSpace(t.Email)) continue;

                await SendAsync(
                    to: t.Email,
                    subject: subject,
                    body: body,
                    attachmentName: attachmentName,
                    attachmentBytes: attachmentBytes,
                    ct: ct);
            }
        }

        // ========= MAINTENANCE STATUS UPDATE Notifications =========
        public async Task SendMaintenanceStatusChangedAsync(string tenantEmail, MaintenanceRequest req, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantEmail)) return;

            var subject = $"[QLT] Cập nhật bảo trì - {req.RoomCode} - {req.StatusVi}";
            var body =
            $@"Xin chào,

            Yêu cầu bảo trì có mã {req.RequestId} cho phòng {req.RoomCode} đã được cập nhật trạng thái: {req.StatusVi}.
            Phân loại: {req.Category}
            Mô tả: {req.Description}
            Chi phí ước tính: {(req.EstimatedCost.HasValue ? req.EstimatedCost.Value.ToString("N0") + " đ" : "(chưa có)")}

            Trân trọng,
            QLT";

            await SendAsync(tenantEmail, subject, body, ct: ct);
        }


        //========== CONTRACT Notifications =========
       
        public async Task SendContractDraftAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenantsAsync(contract,
                subject: $"[QLT] Bản nháp hợp đồng phòng {contract.RoomCode}",
                body: BuildContractBody(contract, "BẢN NHÁP HỢP ĐỒNG", "Vui lòng kiểm tra thông tin và phản hồi nếu cần chỉnh sửa."),
                ct: ct);
        } //SEND DRAFT

        public async Task SendContractPdfFromPathAsync(Contract contract, string pdfPath, CancellationToken ct = default)
        {
            var (name, bytes) = TryLoadAttachment(pdfPath);
            var subject = $"[QLT] Hợp đồng phòng {contract.RoomCode} (PDF đính kèm)";
            var body = BuildContractBody(contract, "HỢP ĐỒNG THUÊ PHÒNG (PDF)", "PDF hợp đồng đính kèm email này.");
            await SendToAllTenantsAsync(contract, subject, body, name, bytes, ct);
        }
        public async Task SendContractPdfAsync(Contract contract, CancellationToken ct = default)
        {
            var (name, bytes) = TryLoadAttachment(contract.PdfUrl);

            var subject = $"[QLT] Hợp đồng phòng {contract.RoomCode} (PDF đính kèm)";
            var note = bytes != null
                ? "PDF hợp đồng đính kèm email này."
                : (string.IsNullOrWhiteSpace(contract.PdfUrl) ? "(Chưa có file PDF đính kèm)" : $"Link (không đính kèm): {contract.PdfUrl}");
            var body = BuildContractBody(contract, "HỢP ĐỒNG THUÊ PHÒNG ĐIỆN TỬ (PDF)", note);

            await SendToAllTenantsAsync(contract, subject, body, name, bytes, ct);
        }
        public async Task SendContractAddendumAsync(Contract parent, ContractAddendum addendum, CancellationToken ct = default)
        {
            var (name, bytes) = TryLoadAttachment(addendum.PdfUrl);

            var subject = $"[QLT] Phụ lục hợp đồng {addendum.AddendumNumber ?? addendum.AddendumId} (PDF đính kèm)";
            var note = bytes != null
                ? "PDF phụ lục hợp đồng đính kèm email này."
                : (string.IsNullOrWhiteSpace(addendum.PdfUrl) ? "(Chưa có PDF đính kèm)" : $"Link (không đính kèm): {addendum.PdfUrl}");
            var body = BuildContractBody(parent, "PHỤ LỤC HỢP ĐỒNG", note);

            await SendToAllTenantsAsync(parent, subject, body, name, bytes, ct);
        }
        public async Task SendContractActivatedAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenantsAsync(contract,
                subject: $"[QLT] Hợp đồng phòng {contract.RoomCode} đã kích hoạt",
                body: BuildContractBody(contract, "HỢP ĐỒNG KÍCH HOẠT", "Hợp đồng đã có hiệu lực. Cảm ơn bạn."),
                ct: ct);
        }

        public async Task SendContractTerminatedAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenantsAsync(contract,
                subject: $"[QLT] Hợp đồng phòng {contract.RoomCode} đã chấm dứt",
                body: BuildContractBody(contract, "HỢP ĐỒNG CHẤM DỨT", "Vui lòng phối hợp bàn giao phòng / tài sản."),
                ct: ct);
        }

        public async Task SendContractAddendumNeededAsync(Contract contract, CancellationToken ct = default)
        {
            await SendToAllTenantsAsync(contract,
                subject: $"[QLT] Phòng {contract.RoomCode} cần phụ lục hợp đồng",
                body: BuildContractBody(contract, "PHỤ LỤC HỢP ĐỒNG", "Có thay đổi trong hợp đồng. Phụ lục cần được lập để cập nhật thông tin."),
                ct: ct);
        }

        public async Task SendPasswordResetAsync(string toEmail, string adminName, string tempPassword)
        {
            var subject = "[QLT] Đặt lại mật khẩu quản trị";
            var body = $"Xin chào {adminName},\n\nMật khẩu tạm thời của bạn là: {tempPassword}\n" +
                       $"Vui lòng đăng nhập và đổi mật khẩu mới trong mục Cài đặt.\n\nThời gian: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.";

            await SendAsync(toEmail, subject, body);
        }

        // ========= Body builder =========

        public string BuildContractBody(Contract c, string heading, string note)
        {
            var sb = new StringBuilder();
            sb.AppendLine(heading);
            sb.AppendLine($"Số HĐ: {c.ContractNumber}");
            sb.AppendLine($"Mã phòng: {c.RoomCode}");
            sb.AppendLine($"Địa chỉ: {c.HouseAddress}");
            sb.AppendLine($"Thời hạn hợp đồng: {c.StartDate:dd/MM/yyyy} - {c.EndDate:dd/MM/yyyy}");
            sb.AppendLine($"Giá thuê phòng: {c.RentAmount:N0} đ / tháng (thu vào ngày {c.DueDay} đầu tháng)");
            sb.AppendLine($"Số tiền đặt cọc: {c.SecurityDeposit:N0} đ");
            sb.AppendLine();
            sb.AppendLine("Danh sách người thuê bao gồm:");
            foreach (var t in c.Tenants)
                sb.AppendLine($"- {t.Name} / {t.Phone} / {t.Email}");
            sb.AppendLine();
            sb.AppendLine(note);
            sb.AppendLine();
            sb.AppendLine("Trân trọng,\nQLT - Phần mềm quản lý thuê");
            return sb.ToString();
        }
    }
}