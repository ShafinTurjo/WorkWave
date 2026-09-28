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
    // These strings are shared with the Blazor frontend — keep them in sync.
    private const string ModeOnline = "Online";
    private const string ModeInPerson = "InPerson";
    private const string ModePhone = "Phone";
    private static readonly string[] ValidModes = { ModeOnline, ModeInPerson, ModePhone };

    private const string Scheduled = "Scheduled";
    private const string Confirmed = "Confirmed";
    private const string Declined = "Declined";
    private const string Cancelled = "Cancelled";

    // Bangladesh Standard Time (UTC+6, no DST) — used when the client doesn't send its offset.
    private const int DefaultUtcOffsetMinutes = 360;

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

        var hasActive = await _db.Interviews
            .AnyAsync(i => i.JobApplicationId == application.Id && i.Status != Cancelled);
        if (hasActive)
        {
            return Conflict(new { message = "An interview is already scheduled for this applicant. Reschedule it instead." });
        }

        var error = Validate(request, out var scheduledUtc, out var location);
        if (error is not null) return BadRequest(new { message = error });

        var interview = new Interview
        {
            JobApplicationId = application.Id,
            ScheduledAt = scheduledUtc,
            DurationMinutes = request.DurationMinutes,
            Mode = request.Mode,
            Location = location,
            Notes = Clean(request.Notes),
            Status = Scheduled,
            CreatedByUserId = CurrentUserId
        };

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync();
        interview.JobApplication = application;

        await NotifyApplicantAsync(interview, request.UtcOffsetMinutes,
            "Interview scheduled", "An interview has been scheduled for your application.");

        return Ok(ToResponse(interview));
    }

    // Reschedule / edit an interview that hasn't been cancelled. Resets it to "Scheduled"
    // so the applicant is asked to confirm the new details again.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<InterviewResponse>> Update(int id, InterviewRequest request)
    {
        var interview = await LoadAsync(id);
        if (interview is null) return NotFound(new { message = $"Interview {id} not found." });
        if (!CanManage(interview.JobApplication!)) return Forbid();

        if (interview.Status == Cancelled)
        {
            return BadRequest(new { message = "A cancelled interview can't be rescheduled. Schedule a new one instead." });
        }

        var error = Validate(request, out var scheduledUtc, out var location);
        if (error is not null) return BadRequest(new { message = error });

        interview.ScheduledAt = scheduledUtc;
        interview.DurationMinutes = request.DurationMinutes;
        interview.Mode = request.Mode;
        interview.Location = location;
        interview.Notes = Clean(request.Notes);
        interview.Status = Scheduled;
        interview.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await NotifyApplicantAsync(interview, request.UtcOffsetMinutes,
            "Interview rescheduled", "Your interview details have been updated.");

        return Ok(ToResponse(interview));
    }

    // Employer / Admin cancels an interview.
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Employer,Admin")]
    public async Task<ActionResult<InterviewResponse>> Cancel(int id, [FromQuery] int? utcOffsetMinutes)
    {
        var interview = await LoadAsync(id);
        if (interview is null) return NotFound(new { message = $"Interview {id} not found." });
        if (!CanManage(interview.JobApplication!)) return Forbid();

        if (interview.Status == Cancelled)
        {
            return BadRequest(new { message = "This interview is already cancelled." });
        }

        interview.Status = Cancelled;
        interview.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await NotifyApplicantAsync(interview, utcOffsetMinutes,
            "Interview cancelled", "Your interview has been cancelled.");

        return Ok(ToResponse(interview));
    }

    // The applicant confirms or declines their interview.
    [HttpPut("{id:int}/respond")]
    [Authorize(Roles = "Worker")]
    public async Task<ActionResult<InterviewResponse>> Respond(int id, InterviewRespondRequest request)
    {
        var interview = await LoadAsync(id);
        if (interview is null) return NotFound(new { message = $"Interview {id} not found." });

        // Only the applicant who owns this application may respond.
        if (interview.JobApplication is null || interview.JobApplication.ApplicantUserId != CurrentUserId)
            return Forbid();

        if (interview.Status == Cancelled)
        {
            return BadRequest(new { message = "This interview has been cancelled." });
        }

        if (interview.ScheduledAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "This interview time has already passed." });
        }

        interview.Status = request.Confirm ? Confirmed : Declined;
        interview.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

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
    // On success, `location` is the cleaned/normalised link, address or phone number.
    private static string? Validate(InterviewRequest r, out DateTime scheduledUtc, out string location)
    {
        location = "";

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

        var value = Clean(r.Location);

        switch (r.Mode)
        {
            case ModeOnline:
            {
                if (value is null)
                    return "Please provide a valid meeting link (e.g. https://meet.google.com/abc-defg-hij).";

                // Be forgiving: "meet.google.com/abc" -> "https://meet.google.com/abc".
                if (!value.Contains("://", StringComparison.Ordinal))
                    value = "https://" + value;

                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    || !uri.Host.Contains('.'))
                {
                    return "Please provide a valid meeting link (e.g. https://meet.google.com/abc-defg-hij).";
                }

                value = uri.ToString();
                break;
            }

            case ModeInPerson:
                if (value is null) return "Please provide the interview address.";
                break;

            case ModePhone:
                if (value is null || value.Count(char.IsDigit) < 5)
                    return "Please provide a valid phone number.";
                break;
        }

        if (value!.Length > 500)
            return "Location / link is too long (max 500 characters).";

        location = value;
        return null;
    }

    private static InterviewResponse ToResponse(Interview i)
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
            Location = i.Location,
            Notes = i.Notes,
            Status = i.Status,
            CreatedAt = DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc)
        };
    }

    // Best-effort email: a failed email must never fail the API request.
    private async Task NotifyApplicantAsync(Interview interview, int? utcOffsetMinutes, string subject, string intro)
    {
        try
        {
            var application = interview.JobApplication;
            if (application is null || string.IsNullOrWhiteSpace(application.ApplicantEmail)) return;

            var job = application.Job;
            string E(string? s) => WebUtility.HtmlEncode(s ?? "");

            var offset = utcOffsetMinutes is >= -840 and <= 840 ? utcOffsetMinutes.Value : DefaultUtcOffsetMinutes;
            var localTime = DateTime.SpecifyKind(interview.ScheduledAt, DateTimeKind.Utc).AddMinutes(offset);
            var sign = offset >= 0 ? "+" : "-";
            var tzLabel = $"UTC{sign}{Math.Abs(offset) / 60:00}:{Math.Abs(offset) % 60:00}";

            var where = interview.Mode switch
            {
                ModeOnline => $"Online — <a href=\"{E(interview.Location)}\">{E(interview.Location)}</a>",
                ModeInPerson => $"In person — {E(interview.Location)}",
                _ => $"Phone call — {E(interview.Location)}"
            };

            var details = interview.Status == Cancelled
                ? ""
                : $"""
                  <p><strong>When:</strong> {localTime:dddd, dd MMM yyyy, hh:mm tt} ({tzLabel})<br/>
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
