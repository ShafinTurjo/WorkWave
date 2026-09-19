using System.Security.Cryptography;
using System.Text;
using Backend.Models;

namespace Backend.Security;

/// <summary>
/// Creates and checks signed, expiring password-reset tokens WITHOUT storing anything
/// in the database (so no migration is needed).
///
/// Token format: {expiryUnixSeconds}.{hmacHex}
/// The HMAC covers the user's id, the expiry AND the user's current password hash.
/// That means:
///   - a token can't be forged or edited (needs the secret key),
///   - a token expires by itself,
///   - a token stops working the moment the password changes (single use).
/// </summary>
public static class PasswordResetToken
{
    public static string Create(User user, string secretKey, TimeSpan lifetime)
    {
        var expiry = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var signature = Sign(user, expiry, secretKey);
        return $"{expiry}.{Convert.ToHexString(signature)}";
    }

    public static bool IsValid(string token, User user, string secretKey)
    {
        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var expiry))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
        {
            return false;
        }

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = Sign(user, expiry, secretKey);
        return CryptographicOperations.FixedTimeEquals(provided, expected);
    }

    private static byte[] Sign(User user, long expiry, string secretKey)
    {
        var data = Encoding.UTF8.GetBytes($"password-reset|{user.Id}|{expiry}|{user.PasswordHash}");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretKey), data);
    }
}
