using Microsoft.JSInterop;

namespace Frontend.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _initialized = false;

    public event Action? OnChange;
    public bool IsDarkMode { get; private set; } = false;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_theme");
        IsDarkMode = saved == "dark";
        await ApplyThemeAsync();
    }

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_theme", IsDarkMode ? "dark" : "light");
        await ApplyThemeAsync();
        OnChange?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        await _js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", IsDarkMode ? "dark" : "light");
    }
}