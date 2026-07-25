using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Email;

/// <summary>
/// Sends over any SMTP relay (Gmail app-password, SES, SendGrid, etc.) via config alone - no
/// vendor SDK. A blank <see cref="SmtpOptions.Host"/> (e.g. local dev with nothing configured)
/// logs and skips the send instead of throwing, so career-site testing doesn't require a real
/// mailbox to be wired up first.
/// </summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogWarning("Smtp:Host is not configured - skipping email to {ToEmail} ({Subject})", toEmail, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var secureSocketOptions = settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.User))
        {
            await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
