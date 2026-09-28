using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Asks the API whether the current user's payment is approved.
/// Admins always pass. Real enforcement is on the server; this only drives the UI.
/// </summary>
public class PaymentAccessService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    public PaymentAccessService(HttpClient http, AuthStateService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<bool> IsApprovedAsync()
    {
        if (_auth.IsAdmin) return true;
        if (!_auth.IsLoggedIn) return false;

        try
        {
            var status = await _http.GetFromJsonAsync<PaymentStatusModel>("api/payment/status");
            return status?.Status == "Approved";
        }
        catch
        {
            return false;
        }
    }
}
