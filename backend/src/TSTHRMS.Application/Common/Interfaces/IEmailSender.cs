namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Outbound transactional email (application acknowledgements today; offer notices, pre-boarding
/// nudges, etc. in later Recruitment slices). Config-driven SMTP behind this so any provider's
/// relay (Gmail, SES, SendGrid) works without a vendor-specific SDK.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
