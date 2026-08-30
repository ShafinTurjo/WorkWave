using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuthController(ApplicationDbContext db)
    {
        _db = db;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
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
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
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

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }
}
