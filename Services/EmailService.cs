using AMS.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendAsync(string to,string subject,string body,string smtpHost,int smtpPort,string smtpUser,string smtpPassword,bool useSsl,string senderName,string senderAddress,CancellationToken ct = default)
        {
            await SendAsync(to, subject, body, smtpHost, smtpPort, smtpUser, smtpPassword, useSsl, senderName, senderAddress, null, null, ct);
        }

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
            string? attachmentFileName,
            byte[]? attachmentBytes,
            CancellationToken ct = default)
        {
            var message = new MimeMessage();
            var fromName = string.IsNullOrWhiteSpace(senderName) ? "QLT" : senderName;
            var fromAddr = string.IsNullOrWhiteSpace(senderAddress) ? smtpUser : senderAddress;

            message.From.Add(new MailboxAddress(fromName, fromAddr));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder { TextBody = body ?? string.Empty };

            if (!string.IsNullOrWhiteSpace(attachmentFileName) && attachmentBytes is { Length: > 0 })
            {
                // Assume PDFs for contract use-cases; MimeKit will still handle generically
                builder.Attachments.Add(attachmentFileName, attachmentBytes, new MimeKit.ContentType("application", "pdf"));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // Choose socket options based on port and "Use SSL/TLS" toggle.
            // - 465: implicit SSL/TLS (SslOnConnect, useSsl=true)
            // - 587: STARTTLS (StartTls or StartTlsWhenAvailable, useSsl=false)
            // - others: StartTlsWhenAvailable (will use TLS if offered)
            SecureSocketOptions socketOptions = GetSocketOptions(smtpPort, useSsl);

            try
            {
                await client.ConnectAsync(smtpHost, smtpPort, socketOptions, ct);
            }
            catch (MailKit.Security.SslHandshakeException) // mismatch between selected mode and server
            {
                // Fallback: try STARTTLS if implicit SSL failed or try Auto if STARTTLS failed
                var fallback = socketOptions switch
                {
                    SecureSocketOptions.SslOnConnect => SecureSocketOptions.StartTls,
                    SecureSocketOptions.StartTls => SecureSocketOptions.StartTlsWhenAvailable,
                    _ => SecureSocketOptions.Auto
                };

                await client.ConnectAsync(smtpHost, smtpPort, fallback, ct);
            }

            // Some servers allow anonymous; only authenticate if username provided
            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                await client.AuthenticateAsync(smtpUser, smtpPassword ?? string.Empty, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }

        private static SecureSocketOptions GetSocketOptions(int port, bool useSslToggle)
        {
            if (port == 465) return SecureSocketOptions.SslOnConnect; // implicit TLS
            if (port == 587) return useSslToggle ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls; // expect STARTTLS when toggle=false
            // For other ports (25 or custom), try StartTls when available
            return useSslToggle ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        }
    }
}