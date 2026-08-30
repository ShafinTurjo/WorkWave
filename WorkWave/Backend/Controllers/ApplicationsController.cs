using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ApplicationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // POST api/applications
    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Apply(ApplyRequest request)
    {
        var jobExists = await _db.Jobs.AnyAsync(j => j.Id == request.JobId);
        if (!jobExists)
        {
            return NotFound(new { message = $"Job {request.JobId} not found." });
        }

        var applicantExists = await _db.Users.AnyAsync(u => u.Id == request.ApplicantUserId);
        if (!applicantExists)
        {
            return BadRequest(new { message = "ApplicantUserId does not match an existing user." });
        }

        var alreadyApplied = await _db.JobApplications
            .AnyAsync(a => a.JobId == request.JobId && a.ApplicantUserId == request.ApplicantUserId);
        if (alreadyApplied)
        {
            return Conflict(new { message = "You have already applied to this job." });
        }

        var application = new JobApplication
        {
            JobId = request.JobId,
            ApplicantUserId = request.ApplicantUserId,
            ApplicantName = request.ApplicantName,
            ApplicantEmail = request.ApplicantEmail,
            CoverLetter = request.CoverLetter
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();

        var job = await _db.Jobs.FindAsync(application.JobId);

        return Ok(new ApplicationResponse
        {
            Id = application.Id,
            JobId = application.JobId,
            JobTitle = job?.Title ?? "",
            ApplicantName = application.ApplicantName,
            ApplicantEmail = application.ApplicantEmail,
            AppliedAt = application.AppliedAt
        });
    }

    // GET api/applications/user/5  (applications submitted by a specific worker)
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<ApplicationResponse>>> GetByUser(int userId)
    {
        var applications = await _db.JobApplications
            .Include(a => a.Job)
            .Where(a => a.ApplicantUserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return Ok(applications.Select(a => new ApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            JobTitle = a.Job?.Title ?? "",
            ApplicantName = a.ApplicantName,
            ApplicantEmail = a.ApplicantEmail,
            AppliedAt = a.AppliedAt
        }).ToList());
    }

    // GET api/applications/job/5  (applications received for a specific job)
    [HttpGet("job/{jobId:int}")]
    public async Task<ActionResult<List<ApplicationResponse>>> GetByJob(int jobId)
    {
        var applications = await _db.JobApplications
            .Include(a => a.Job)
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return Ok(applications.Select(a => new ApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            JobTitle = a.Job?.Title ?? "",
            ApplicantName = a.ApplicantName,
            ApplicantEmail = a.ApplicantEmail,
            AppliedAt = a.AppliedAt
        }).ToList());
    }
}
