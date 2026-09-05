using Backend.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend.Services;

// Sends email via SMTP using MailKit (System.Net.Mail.SmtpClient is obsolete and has
// known STARTTLS/AUTH compatibility problems with Gmail).
// If email isn't configured (or sending fails), this logs a warning and does nothing —
// it never throws, so a misconfigured or unreachable mail server can't break applying
// for a job or updating an application's status.
public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string bodyHtml)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email notifications are disabled (Email:Enabled=false in appsettings). Skipped sending '{Subject}' to {ToEmail}.", subject, toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            _logger.LogWarning("Email settings are incomplete (SmtpHost/SenderEmail missing). Skipped sending '{Subject}' to {ToEmail}.", subject, toEmail);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = bodyHtml };

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.SenderEmail, _options.SenderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Sent email '{Subject}' to {ToEmail}.", subject, toEmail);
        }
        catch (Exception ex)
        {
            // Never let a failed email break the actual API request (apply / status update).
            _logger.LogWarning(ex, "Failed to send email '{Subject}' to {ToEmail}.", subject, toEmail);
        }
    }
}
