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

    // GET api/jobs
    [HttpGet]
    public async Task<ActionResult<List<JobResponse>>> GetAll()
    {
        var jobs = await _db.Jobs
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
            PostedByUserId = request.PostedByUserId
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
        Tags = string.IsNullOrWhiteSpace(job.TagsCsv)
            ? Array.Empty<string>()
            : job.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        PostedAt = job.PostedAt
    };
}
