using AMS.Services.Interfaces;
using DocumentFormat.OpenXml.Vml;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendAsync(
            string to,
            string subject,
            string body,
            string smtpHost,
            int smtpPort,
            string smtpUser,
            string smtpPassword,
            bool useSsl,
            string senderName,
            string senderAddress,
            CancellationToken ct = default)
        {
            var message = new MimeMessage();
            var from = new MailboxAddress(string.IsNullOrWhiteSpace(senderName) ? "AMS" : senderName,
                                          string.IsNullOrWhiteSpace(senderAddress) ? smtpUser : senderAddress);
            message.From.Add(from);
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            var secure = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await client.ConnectAsync(smtpHost, smtpPort, secure, ct);
            if (!string.IsNullOrWhiteSpace(smtpUser))
                await client.AuthenticateAsync(smtpUser, smtpPassword, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}