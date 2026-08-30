using Microsoft.JSInterop;

namespace Frontend.Services;

public class AuthStateService
{
    private readonly IJSRuntime _js;
    private bool _initialized = false;

    public event Action? OnChange;

    public int? UserId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsLoggedIn => UserId is not null;
    public bool IsAdmin => Role == "Admin";

    // Admin can access every role's area; otherwise the role must match exactly.
    public bool CanAccess(string requiredRole) => IsAdmin || Role == requiredRole;

    public AuthStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        var savedId = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_user_id");
        if (int.TryParse(savedId, out var id))
        {
            UserId = id;
            FullName = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_user_name");
            Email = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_user_email");
            Role = await _js.InvokeAsync<string?>("localStorage.getItem", "workwave_user_role");
        }
    }

    public async Task SetUserAsync(int userId, string fullName, string email, string role)
    {
        UserId = userId;
        FullName = fullName;
        Email = email;
        Role = role;

        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_user_id", userId.ToString());
        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_user_name", fullName);
        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_user_email", email);
        await _js.InvokeVoidAsync("localStorage.setItem", "workwave_user_role", role);

        OnChange?.Invoke();
    }

    public async Task LogoutAsync()
    {
        UserId = null;
        FullName = null;
        Email = null;
        Role = null;

        await _js.InvokeVoidAsync("localStorage.removeItem", "workwave_user_id");
        await _js.InvokeVoidAsync("localStorage.removeItem", "workwave_user_name");
        await _js.InvokeVoidAsync("localStorage.removeItem", "workwave_user_email");
        await _js.InvokeVoidAsync("localStorage.removeItem", "workwave_user_role");

        OnChange?.Invoke();
    }
}