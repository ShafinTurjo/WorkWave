using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class RegisterRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    // Only "Worker" or "Employer" allowed at self-registration; Admin is seeded separately.
    [Required]
    public string Role { get; set; } = "Worker";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";
}

public class RegisterResponse
{
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Email { get; set; } = "";
}

public class VerifyEmailRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";
}

public class ResendVerificationRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}
