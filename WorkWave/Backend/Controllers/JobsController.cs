using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public JobsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET api/jobs (শুধুমাত্র Active চাকরিগুলো দেখাবে)
    [HttpGet]
    public async Task<ActionResult<List<JobResponse>>> GetAll()
    {
        var jobs = await _db.Jobs
            .Where(j => j.IsActive)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    // GET api/jobs/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobResponse>> GetById(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        return Ok(ToResponse(job));
    }

    // GET api/jobs/mine/5 (একক নিয়োগকর্তার সব Active ও Closed চাকরিগুলো দেখাবে)
    [HttpGet("mine/{userId:int}")]
    public async Task<ActionResult<List<JobResponse>>> GetMine(int userId)
    {
        var jobs = await _db.Jobs
            .Where(j => j.PostedByUserId == userId)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    // PUT api/jobs/5/status (স্ট্যাটাস Active/Closed করার জন্য)
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] UpdateJobStatusRequest request)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var requester = await _db.Users.FindAsync(request.RequestingUserId);
        var isOwner = job.PostedByUserId == request.RequestingUserId;
        var isAdmin = requester is not null && requester.Role == "Admin";

        if (!isOwner && !isAdmin) return Forbid();

        job.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE api/jobs/5?requestingUserId=3
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int requestingUserId)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { message = $"Job {id} not found." });

        var requester = await _db.Users.FindAsync(requestingUserId);
        var isOwner = job.PostedByUserId == requestingUserId;
        var isAdmin = requester is not null && requester.Role == "Admin";
        if (!isOwner && !isAdmin) return Forbid();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/jobs
    [HttpPost]
    public async Task<ActionResult<JobResponse>> Create(JobCreateRequest request)
    {
        var posterExists = await _db.Users.AnyAsync(u => u.Id == request.PostedByUserId);
        if (!posterExists)
        {
            return BadRequest(new { message = "PostedByUserId does not match an existing user." });
        }

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
            PostedByUserId = request.PostedByUserId,
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