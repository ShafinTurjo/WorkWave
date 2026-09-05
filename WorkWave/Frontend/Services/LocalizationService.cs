using Microsoft.JSInterop;

namespace Frontend.Services;

public class LocalizationService
{
    private readonly IJSRuntime _js;
    private bool _initialized = false;

    public string CurrentLang { get; private set; } = "en";
    public event Action? OnChange;

    public LocalizationService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_lang");
        CurrentLang = saved ?? "en";
    }

    public string T(string key)
    {
        if (Translations.Data.TryGetValue(CurrentLang, out var dict)
            && dict.TryGetValue(key, out var val))
        {
            return val;
        }
        return key;
    }

    public async Task SetLanguageAsync(string lang)
    {
        CurrentLang = lang;
        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_lang", lang);
        OnChange?.Invoke();
    }
}