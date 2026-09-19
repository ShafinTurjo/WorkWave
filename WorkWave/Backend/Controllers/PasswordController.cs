using System.ComponentModel.DataAnnotations;
using Backend.Data;
using Backend.Models;
using Backend.Security;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/password")]
public class PasswordController : ControllerBase
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public PasswordController(ApplicationDbContext db, IEmailService emailService, IConfiguration config)
    {
        _db = db;
        _emailService = emailService;
        _config = config;
    }

    // POST api/password/forgot
    [HttpPost("forgot")]
    public async Task<ActionResult> Forgot(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is not null)
        {
            await SendResetEmailAsync(user);
        }

        // Don't reveal whether the account exists — always return the same message.
        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    // POST api/password/reset
    [HttpPost("reset")]
    public async Task<ActionResult> Reset(ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        var secretKey = _config["Jwt:Key"] ?? "";

        if (user is null || !PasswordResetToken.IsValid(request.Token, user, secretKey))
        {
            return BadRequest(new { message = "This reset link is invalid or has expired. Please request a new one." });
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Your password has been reset. You can now log in." });
    }

    private async Task SendResetEmailAsync(User user)
    {
        var frontendBaseUrl = (_config["FrontendBaseUrl"] ?? "https://localhost:7174").TrimEnd('/');
        var secretKey = _config["Jwt:Key"] ?? "";
        var token = PasswordResetToken.Create(user, secretKey, ResetTokenLifetime);
        var link = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

        var subject = "Reset your WorkWave password";
        var body = $"""
            <p>Hi {user.FullName},</p>
            <p>We received a request to reset your WorkWave password. Click the link below to choose a new one:</p>
            <p><a href="{link}">Reset my password</a></p>
            <p>This link expires in 1 hour and stops working once your password is changed. If you didn't ask for this, you can safely ignore this email.</p>
            <p>— WorkWave</p>
            """;

        await _emailService.SendAsync(user.Email, user.FullName, subject, body);
    }
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = "";
}
