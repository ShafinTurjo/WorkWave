using System.Net;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterviewsController : ApiControllerBase
{
    private static readonly string[] ValidModes = { "Online", "Onsite", "Phone" };
    private static readonly string[] ValidStatuses = { "Completed", "Cancelled" };

    private const string Scheduled = "Scheduled";

    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<InterviewsController> _logger;

    public InterviewsController(ApplicationDbContext db, IEmailService emailService, ILogger<InterviewsController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    // Employer (job owner) or Admin schedules an interview for one application.
    [HttpPost]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<InterviewResponse>> Create(InterviewRequest request)
    {
        var application = await _db.JobApplications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);
        if (application is null) return NotFound(new { message = $"Application {request.ApplicationId} not found." });

        if (!CanManage(application)) return Forbid();

        if (application.Status == "Rejected")
        {
            return BadRequest(new { message = "You can't schedule an interview for a rejected application." });
        }

        var hasScheduled = await _db.Interviews
            .AnyAsync(i => i.JobApplicationId == application.Id && i.Status == Scheduled);
        if (hasScheduled)
        {
            return Conflict(new { message = "An interview is already scheduled for this applicant. Reschedule it instead." });
        }

        var error = Validate(request, out var scheduledUtc);
        if (error is not null) return BadRequest(new { message = error });

        var interview = new Interview
        {
            JobApplicationId = application.Id,
            ScheduledAt = scheduledUtc,
            DurationMinutes = request.DurationMinutes,
            Mode = request.Mode,
            LocationOrLink = Clean(request.LocationOrLink),
            Notes = Clean(request.Notes),
            Status = Scheduled,
            CreatedByUserId = CurrentUserId
        };

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync();
        interview.JobApplication = application;

        await NotifyApplicantAsync(interview, "Interview scheduled", "An interview has been scheduled for your application.");

        return Ok(ToResponse(interview));
    }

    // Reschedule / edit a still-scheduled interview.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<InterviewResponse>> Update(int id, InterviewRequest request)
    {
        var interview = await LoadAsync(id);
        if (interview is null) return NotFound(new { message = $"Interview {id} not found." });
        if (!CanManage(interview.JobApplication!)) return Forbid();

        if (interview.Status != Scheduled)
        {
            return BadRequest(new { message = "Only a scheduled interview can be rescheduled." });
        }

        var error = Validate(request, out var scheduledUtc);
        if (error is not null) return BadRequest(new { message = error });

        interview.ScheduledAt = scheduledUtc;
        interview.DurationMinutes = request.DurationMinutes;
        interview.Mode = request.Mode;
        interview.LocationOrLink = Clean(request.LocationOrLink);
        interview.Notes = Clean(request.Notes);
        await _db.SaveChangesAsync();

        await NotifyApplicantAsync(interview, "Interview rescheduled", "Your interview details have been updated.");

        return Ok(ToResponse(interview));
    }

    // Mark a scheduled interview as Completed or Cancelled.
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<InterviewResponse>> UpdateStatus(int id, UpdateInterviewStatusRequest request)
    {
        if (!ValidStatuses.Contains(request.Status))
        {
            return BadRequest(new { message = $"Status must be one of: {string.Join(", ", ValidStatuses)}." });
        }

        var interview = await LoadAsync(id);
        if (interview is null) return NotFound(new { message = $"Interview {id} not found." });
        if (!CanManage(interview.JobApplication!)) return Forbid();

        if (interview.Status != Scheduled)
        {
            return BadRequest(new { message = "Only a scheduled interview can be updated." });
        }

        interview.Status = request.Status;
        await _db.SaveChangesAsync();

        if (request.Status == "Cancelled")
        {
            await NotifyApplicantAsync(interview, "Interview cancelled", "Your interview has been cancelled.");
        }

        return Ok(ToResponse(interview));
    }

    // All interviews for one job (used by the employer's applicants table).
    [HttpGet("job/{jobId:int}")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<List<InterviewResponse>>> GetByJob(int jobId)
    {
        var job = await _db.Jobs.FindAsync(jobId);
        if (job is null) return NotFound(new { message = $"Job {jobId} not found." });
        if (job.PostedByUserId != CurrentUserId && !IsAdmin) return Forbid();

        var items = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a!.Job)
            .Where(i => i.JobApplication!.JobId == jobId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync();

        return Ok(items.Select(ToResponse).ToList());
    }

    // The logged-in worker's own interviews.
    [HttpGet("mine")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<List<InterviewResponse>>> GetMine()
    {
        var userId = CurrentUserId;

        var items = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a!.Job)
            .Where(i => i.JobApplication!.ApplicantUserId == userId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync();

        return Ok(items.Select(ToResponse).ToList());
    }

    // ---------- helpers ----------

    private bool CanManage(JobApplication application) =>
        IsAdmin || (application.Job is not null && application.Job.PostedByUserId == CurrentUserId);

    private Task<Interview?> LoadAsync(int id) =>
        _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a!.Job)
            .FirstOrDefaultAsync(i => i.Id == id);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Returns an error message, or null when the request is valid.
    private static string? Validate(InterviewRequest r, out DateTime scheduledUtc)
    {
        scheduledUtc = r.ScheduledAt.Kind switch
        {
            DateTimeKind.Utc => r.ScheduledAt,
            DateTimeKind.Local => r.ScheduledAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(r.ScheduledAt, DateTimeKind.Utc)
        };

        if (scheduledUtc <= DateTime.UtcNow)
            return "Interview time must be in the future.";

        if (r.DurationMinutes < 15 || r.DurationMinutes > 480)
            return "Duration must be between 15 and 480 minutes.";

        if (!ValidModes.Contains(r.Mode))
            return $"Mode must be one of: {string.Join(", ", ValidModes)}.";

        var location = Clean(r.LocationOrLink);

        if (r.Mode == "Online")
        {
            // Only real http(s) links — the frontend renders this as a clickable link.
            if (location is null || !Uri.TryCreate(location, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return "Please provide a valid meeting link (starting with http:// or https://).";
            }
        }
        else if (r.Mode == "Onsite" && location is null)
        {
            return "Please provide the interview address.";
        }

        return null;
    }

    private InterviewResponse ToResponse(Interview i)
    {
        var application = i.JobApplication;
        var job = application?.Job;

        return new InterviewResponse
        {
            Id = i.Id,
            ApplicationId = i.JobApplicationId,
            JobId = application?.JobId ?? 0,
            JobTitle = job?.Title ?? "",
            Company = job?.Company ?? "",
            ApplicantName = application?.ApplicantName ?? "",
            ApplicantEmail = application?.ApplicantEmail ?? "",
            ScheduledAt = DateTime.SpecifyKind(i.ScheduledAt, DateTimeKind.Utc),
            DurationMinutes = i.DurationMinutes,
            Mode = i.Mode,
            LocationOrLink = i.LocationOrLink,
            Notes = i.Notes,
            Status = i.Status,
            CreatedAt = DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc)
        };
    }

    // Best-effort email: a failed email must never fail the API request.
    private async Task NotifyApplicantAsync(Interview interview, string subject, string intro)
    {
        try
        {
            var application = interview.JobApplication;
            if (application is null || string.IsNullOrWhiteSpace(application.ApplicantEmail)) return;

            var job = application.Job;
            string E(string? s) => WebUtility.HtmlEncode(s ?? "");

            // Bangladesh Standard Time is a fixed UTC+6 (no daylight saving).
            var bdTime = DateTime.SpecifyKind(interview.ScheduledAt, DateTimeKind.Utc).AddHours(6);

            var where = interview.Mode switch
            {
                "Online" => $"Online — <a href=\"{E(interview.LocationOrLink)}\">{E(interview.LocationOrLink)}</a>",
                "Onsite" => $"On-site — {E(interview.LocationOrLink)}",
                _ => $"Phone{(string.IsNullOrEmpty(interview.LocationOrLink) ? "" : " — " + E(interview.LocationOrLink))}"
            };

            var details = interview.Status == "Cancelled"
                ? ""
                : $"""
                  <p><strong>When:</strong> {bdTime:dddd, dd MMM yyyy, hh:mm tt} (Bangladesh time, UTC+6)<br/>
                  <strong>Duration:</strong> {interview.DurationMinutes} minutes<br/>
                  <strong>Where:</strong> {where}</p>
                  {(string.IsNullOrEmpty(interview.Notes) ? "" : $"<p><strong>Note from the employer:</strong> {E(interview.Notes)}</p>")}
                  """;

            var body = $"""
                <p>Hi {E(application.ApplicantName)},</p>
                <p>{E(intro)}</p>
                <p><strong>Job:</strong> {E(job?.Title)} at {E(job?.Company)}</p>
                {details}
                <p>— WorkWave</p>
                """;

            await _emailService.SendAsync(
                application.ApplicantEmail,
                application.ApplicantName,
                $"{subject}: {job?.Title}",
                body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send interview notification email for interview {InterviewId}.", interview.Id);
        }
    }
}
