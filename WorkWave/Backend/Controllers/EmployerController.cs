using Backend.Data;
using Backend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmployerController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployerController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats/{userId}")]
    public async Task<ActionResult<EmployerDashboardStatsDto>> GetEmployerStats(int userId)
    {
        var denied = EnsureSelfOrAdmin(userId);
        if (denied is not null) return denied;

        var employerJobs = _context.Jobs.Where(j => j.PostedByUserId == userId);

        var totalJobs = await employerJobs.CountAsync();

        
        var activeJobs = await employerJobs.CountAsync(j => j.IsActive && !j.IsFlagged);
        var closedJobs = await employerJobs.CountAsync(j => !j.IsActive && !j.IsFlagged);

        var totalApplicants = await _context.JobApplications
            .Where(a => employerJobs.Select(j => j.Id).Contains(a.JobId))
            .CountAsync();

        var stats = new EmployerDashboardStatsDto
        {
            TotalJobs = totalJobs,
            TotalApplicants = totalApplicants,
            ActiveJobs = activeJobs,
            ClosedJobs = closedJobs
        };

        return Ok(stats);
    }
}