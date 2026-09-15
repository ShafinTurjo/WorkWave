using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public JobsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET api/jobs (শুধুমাত্র Active চাকরিগুলো দেখাবে) — publicly browsable, no auth needed
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<JobResponse>>> GetAll()
    {
        var jobs = await _db.Jobs
            .Where(j => j.IsActive)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    // GET api/jobs/5 — publicly viewable
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<JobResponse>> GetById(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        return Ok(ToResponse(job));
    }

    // GET api/jobs/mine/5 (একক নিয়োগকর্তার সব Active ও Closed চাকরিগুলো দেখাবে)
    [HttpGet("mine/{userId:int}")]
    [Authorize]
    public async Task<ActionResult<List<JobResponse>>> GetMine(int userId)
    {
        var denied = EnsureSelfOrAdmin(userId);
        if (denied is not null) return denied;

        var jobs = await _db.Jobs
            .Where(j => j.PostedByUserId == userId)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    // PUT api/jobs/5/status (স্ট্যাটাস Active/Closed করার জন্য)
    [HttpPut("{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] UpdateJobStatusRequest request)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var isOwner = job.PostedByUserId == CurrentUserId;
        if (!isOwner && !IsAdmin) return Forbid();

        job.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE api/jobs/5
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var isOwner = job.PostedByUserId == CurrentUserId;
        if (!isOwner && !IsAdmin) return Forbid();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/jobs
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<JobResponse>> Create(JobCreateRequest request)
    {
        var job = new Job
        {
            Title = request.Title,
            Company = request.Company,
            Location = request.Location,
            Category = request.Category,
            Salary = request.Salary,
            Description = request.Description,
            JobType = request.JobType,
            TagsCsv = string.Join(",", request.Tags),
            PostedByUserId = CurrentUserId, // always the authenticated user, never client-supplied
            IsActive = true
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToResponse(job));
    }

    private static JobResponse ToResponse(Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        Company = job.Company,
        Location = job.Location,
        Category = job.Category,
        Salary = job.Salary,
        Description = job.Description,
        JobType = job.JobType,
        IsActive = job.IsActive,
        Tags = string.IsNullOrWhiteSpace(job.TagsCsv)
            ? Array.Empty<string>()
            : job.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        PostedAt = job.PostedAt
    };
}
