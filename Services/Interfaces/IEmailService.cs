using System.Threading;
using System.Threading.Tasks;

namespace AMS.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(
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
            CancellationToken ct = default);
    }
}