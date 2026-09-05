using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Security;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    public AuthController(ApplicationDbContext db, IEmailService emailService, IConfiguration config)
    {
        _db = db;
        _emailService = emailService;
        _config = config;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        // Self-registration can only create Worker or Employer accounts.
        var role = request.Role == "Employer" ? "Employer" : "Worker";

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(VerificationTokenLifetime)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SendVerificationEmailAsync(user);

        return Ok(new RegisterResponse
        {
            Email = user.Email,
            Message = "Account created. Please check your email to verify your address before logging in."
        });
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.IsEmailVerified && user.Role != "Admin")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Please verify your email address before logging in. Check your inbox for the verification link.",
                code = "EMAIL_NOT_VERIFIED"
            });
        }

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    // GET api/auth/verify-email?email=...&token=...
    [HttpGet("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromQuery] VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            return BadRequest(new { message = "Invalid verification link." });
        }

        if (user.IsEmailVerified)
        {
            return Ok(new { message = "Your email is already verified. You can log in." });
        }

        var tokenValid = user.EmailVerificationToken == request.Token
                          && user.EmailVerificationTokenExpiresAt.HasValue
                          && user.EmailVerificationTokenExpiresAt.Value > DateTime.UtcNow;
        if (!tokenValid)
        {
            return BadRequest(new { message = "This verification link is invalid or has expired. Please request a new one." });
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email verified successfully! You can now log in." });
    }

    // POST api/auth/resend-verification
    [HttpPost("resend-verification")]
    public async Task<ActionResult> ResendVerification(ResendVerificationRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Don't reveal whether the account exists — always return the same generic message.
        if (user is not null && !user.IsEmailVerified)
        {
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(VerificationTokenLifetime);
            await _db.SaveChangesAsync();
            await SendVerificationEmailAsync(user);
        }

        return Ok(new { message = "If an account with that email needs verifying, a new link has been sent." });
    }

    private async Task SendVerificationEmailAsync(User user)
    {
        var frontendBaseUrl = _config["FrontendBaseUrl"] ?? "https://localhost:7174";
        var link = $"{frontendBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={user.EmailVerificationToken}";

        var subject = "Verify your WorkWave email address";
        var body = $"""
            <p>Hi {user.FullName},</p>
            <p>Thanks for signing up for WorkWave. Please confirm your email address by clicking the link below:</p>
            <p><a href="{link}">Verify my email</a></p>
            <p>This link expires in 24 hours. If you didn't create a WorkWave account, you can ignore this email.</p>
            <p>— WorkWave</p>
            """;

        await _emailService.SendAsync(user.Email, user.FullName, subject, body);
    }
}
