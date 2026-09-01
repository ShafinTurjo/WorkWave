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
    private readonly IWebHostEnvironment _env;

    private const long MaxResumeSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string ResumeUploadsRelativePath = "uploads/resumes";

    public ApplicationsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // POST api/applications  (multipart/form-data: JobId, ApplicantUserId, ApplicantName, ApplicantEmail, CoverLetter, Resume)
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxResumeSizeBytes + 1024)]
    public async Task<ActionResult<ApplicationResponse>> Apply([FromForm] ApplyRequest request)
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

        string? storedFileName = null;
        string? originalFileName = null;

        if (request.Resume is not null)
        {
            var file = request.Resume;

            if (file.Length > MaxResumeSizeBytes)
            {
                return BadRequest(new { message = "Resume must be 5 MB or smaller." });
            }

            var extension = Path.GetExtension(file.FileName);
            var isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                        && extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdf)
            {
                return BadRequest(new { message = "Resume must be a PDF file." });
            }

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                // WebRootPath is only populated automatically if a wwwroot folder exists at startup.
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var uploadsDir = Path.Combine(webRoot, "uploads", "resumes");
            Directory.CreateDirectory(uploadsDir);

            storedFileName = $"{Guid.NewGuid()}.pdf";
            originalFileName = file.FileName;

            var fullPath = Path.Combine(uploadsDir, storedFileName);
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }
        }

        var application = new JobApplication
        {
            JobId = request.JobId,
            ApplicantUserId = request.ApplicantUserId,
            ApplicantName = request.ApplicantName,
            ApplicantEmail = request.ApplicantEmail,
            CoverLetter = request.CoverLetter,
            ResumeFileName = storedFileName,
            ResumeOriginalFileName = originalFileName
        };

        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();

        var job = await _db.Jobs.FindAsync(application.JobId);

        return Ok(ToResponse(application, job?.Title ?? ""));
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

        return Ok(applications.Select(a => ToResponse(a, a.Job?.Title ?? "")).ToList());
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

        return Ok(applications.Select(a => ToResponse(a, a.Job?.Title ?? "")).ToList());
    }

    private static readonly string[] ValidStatuses = { "Pending", "Accepted", "Rejected" };

    // PUT api/applications/5/status  (accept/reject an applicant; only the job's poster or an Admin may do this)
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(int id, UpdateApplicationStatusRequest request)
    {
        if (!ValidStatuses.Contains(request.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", ValidStatuses)}." });
        }

        var application = await _db.JobApplications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (application is null) return NotFound(new { message = $"Application {id} not found." });

        var requester = await _db.Users.FindAsync(request.RequestingUserId);
        var isJobOwner = application.Job is not null && application.Job.PostedByUserId == request.RequestingUserId;
        var isAdmin = requester is not null && requester.Role == "Admin";
        if (!isJobOwner && !isAdmin) return Forbid();

        application.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(application, application.Job?.Title ?? ""));
    }

    private ApplicationResponse ToResponse(JobApplication a, string jobTitle)
    {
        string? resumeUrl = null;
        if (!string.IsNullOrEmpty(a.ResumeFileName))
        {
            resumeUrl = $"{Request.Scheme}://{Request.Host}/{ResumeUploadsRelativePath}/{a.ResumeFileName}";
        }

        return new ApplicationResponse
        {
            Id = a.Id,
            JobId = a.JobId,
            JobTitle = jobTitle,
            ApplicantName = a.ApplicantName,
            ApplicantEmail = a.ApplicantEmail,
            AppliedAt = a.AppliedAt,
            Status = a.Status,
            ResumeOriginalFileName = a.ResumeOriginalFileName,
            ResumeUrl = resumeUrl
        };
    }
}
