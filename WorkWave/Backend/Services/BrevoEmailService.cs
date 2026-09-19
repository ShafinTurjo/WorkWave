using System.Net.Http.Json;
using Backend.Options;
using Microsoft.Extensions.Options;

namespace Backend.Services;

// Sends email through Brevo's HTTPS API (port 443).
// Render's free tier blocks outbound SMTP ports (25/465/587), so the old SMTP
// service can't work there. HTTPS is never blocked.
//
// Settings (Render environment variables):
//   Email__Enabled      = true
//   Email__SenderEmail  = the sender address you verified in Brevo
//   Email__SenderName   = WorkWave
//   Brevo__ApiKey       = your Brevo API key (starts with xkeysib-)
//
// Like the old service, this never throws: a failed email must not break
// registration or any other API request. Problems are written to the log instead.
public class BrevoEmailService : IEmailService
{
    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly string _apiKey;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(
        HttpClient http,
        IOptions<EmailOptions> options,
        IConfiguration config,
        ILogger<BrevoEmailService> logger)
    {
        _http = http;
        _options = options.Value;
        _apiKey = config["Brevo:ApiKey"] ?? "";
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string bodyHtml)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email notifications are disabled (Email:Enabled=false). Skipped sending '{Subject}' to {ToEmail}.",
                subject, toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            _logger.LogWarning(
                "Email settings are incomplete (Brevo:ApiKey or Email:SenderEmail missing). Skipped sending '{Subject}' to {ToEmail}.",
                subject, toEmail);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint);
            request.Headers.Add("api-key", _apiKey);
            request.Content = JsonContent.Create(new
            {
                sender = new { name = _options.SenderName, email = _options.SenderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = bodyHtml
            });

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sent email '{Subject}' to {ToEmail} via Brevo.", subject, toEmail);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Brevo rejected email '{Subject}' to {ToEmail}. Status {Status}. Response: {Body}",
                    subject, toEmail, (int)response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            // Never let a failed email break the actual API request.
            _logger.LogWarning(ex, "Failed to send email '{Subject}' to {ToEmail} via Brevo.", subject, toEmail);
        }
    }
}
