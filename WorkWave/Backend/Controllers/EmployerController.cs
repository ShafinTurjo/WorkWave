using Backend.Data;
using Backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployerController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats/{userId}")]
    public async Task<ActionResult<EmployerDashboardStatsDto>> GetEmployerStats(int userId)
    {
       
        var employerJobs = _context.Jobs.Where(j => j.PostedByUserId == userId);

        var totalJobs = await employerJobs.CountAsync();

        var activeJobs = await employerJobs.CountAsync();
        var closedJobs = 0;

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
