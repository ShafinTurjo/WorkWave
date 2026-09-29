using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Backend.Options;
using Microsoft.Extensions.Options;

namespace Backend.Services;

// Sends email through Mailjet's HTTPS API (port 443), so it works on Render's free tier.
//
// Render environment variables:
//   Email__Enabled      = true
//   Email__SenderEmail  = the sender address you verified in Mailjet
//   Email__SenderName   = WorkWave
//   Mailjet__ApiKey     = Mailjet API Key
//   Mailjet__ApiSecret  = Mailjet Secret Key
//
// Never throws: a failed email must not break registration or any other API request.
public class MailjetEmailService : IEmailService
{
    private const string Endpoint = "https://api.mailjet.com/v3.1/send";

    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(
        HttpClient http,
        IOptions<EmailOptions> options,
        IConfiguration config,
        ILogger<MailjetEmailService> logger)
    {
        _http = http;
        _options = options.Value;
        _apiKey = (config["Mailjet:ApiKey"] ?? "").Trim();
        _apiSecret = (config["Mailjet:ApiSecret"] ?? "").Trim();
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

        if (string.IsNullOrWhiteSpace(_apiKey) ||
            string.IsNullOrWhiteSpace(_apiSecret) ||
            string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            _logger.LogWarning(
                "Email settings incomplete (Mailjet:ApiKey / Mailjet:ApiSecret / Email:SenderEmail missing). Skipped '{Subject}' to {ToEmail}.",
                subject, toEmail);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Content = JsonContent.Create(new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new { Email = _options.SenderEmail, Name = _options.SenderName },
                        To = new[] { new { Email = toEmail, Name = toName } },
                        Subject = subject,
                        HTMLPart = bodyHtml
                    }
                }
            });

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sent email '{Subject}' to {ToEmail} via Mailjet.", subject, toEmail);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Mailjet rejected email '{Subject}' to {ToEmail}. Status {Status}. Response: {Body}",
                    subject, toEmail, (int)response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email '{Subject}' to {ToEmail} via Mailjet.", subject, toEmail);
        }
    }
}
