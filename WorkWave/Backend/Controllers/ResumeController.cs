using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumeController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResumeController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetResume(int userId)
    {
        var denied = EnsureSelfOrAdmin(userId);
        if (denied is not null) return denied;

        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId);
        if (resume == null) return NotFound();
        return Ok(resume);
    }

    [HttpPost]
    public async Task<IActionResult> SaveResume([FromBody] Resume resume)
    {
        var denied = EnsureSelfOrAdmin(resume.UserId);
        if (denied is not null) return denied;

        var existing = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == resume.UserId);
        if (existing != null)
        {
            existing.FullName = resume.FullName;
            existing.Phone = resume.Phone;
            existing.Summary = resume.Summary;
            existing.Experience = resume.Experience;
            existing.Education = resume.Education;
            existing.Skills = resume.Skills;
        }
        else
        {
            _db.Resumes.Add(resume);
        }

        await _db.SaveChangesAsync();
        return Ok(resume);
    }
}