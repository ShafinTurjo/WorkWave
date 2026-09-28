using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Blazor WebAssembly runs with TimeZoneInfo.Local == UTC, so DateTime.ToLocalTime()
/// does not give the user's real local time. This asks the browser for its UTC offset
/// once and converts between UTC (what the API stores) and the user's local time.
/// </summary>
public class LocalTimeService
{
    private readonly IJSRuntime _js;
    private bool _initialized;

    public LocalTimeService(IJSRuntime js) => _js = js;

    /// <summary>Minutes east of UTC (Dhaka = 360).</summary>
    public int OffsetMinutes { get; private set; }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            OffsetMinutes = await _js.InvokeAsync<int>("eval", "0 - new Date().getTimezoneOffset()");
        }
        catch
        {
            OffsetMinutes = 0;
        }
        _initialized = true;
    }

    public DateTime ToLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddMinutes(OffsetMinutes);

    public DateTime ToUtc(DateTime local) =>
        DateTime.SpecifyKind(local.AddMinutes(-OffsetMinutes), DateTimeKind.Utc);
}
