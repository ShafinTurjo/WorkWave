namespace Backend.Options;

public class JwtOptions
{
    // Secret signing key. NEVER put the real value in appsettings.json —
    // set it via user-secrets locally and an environment variable in production.
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "WorkWave";
    public string Audience { get; set; } = "WorkWaveClient";
    public int ExpiryMinutes { get; set; } = 1440; // 24 hours
}
