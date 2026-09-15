using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Reads the saved JWT straight from localStorage (not from AuthStateService) to avoid
/// a circular DI dependency between the handler and the scoped HttpClient it wraps,
/// and attaches it as a Bearer token on every request to the API.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;

    public AuthHeaderHandler(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
