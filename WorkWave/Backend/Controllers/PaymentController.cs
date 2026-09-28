using Backend.Data;
using Backend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public PaymentController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitPayment([FromBody] PaymentSubmitDtos dto)
    {
        var trxId = dto.TransactionId?.Trim();
        if (string.IsNullOrWhiteSpace(trxId))
        {
            return BadRequest(new { message = "Transaction ID is must." });
        }

        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User has not been found." });

        if (user.PaymentStatus == "Approved")
        {
            return BadRequest(new { message = "Your payment has already been approved." });
        }

        // Same trxId keu duibar use korte parbe na
        var alreadyUsed = await _db.Users.AnyAsync(u => u.Id != user.Id && u.TransactionId == trxId);
        if (alreadyUsed)
        {
            return Conflict(new { message = "This Transaction ID has already been used by another account." });
        }

        user.TransactionId = trxId;
        user.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "bKash" : dto.PaymentMethod.Trim();
        user.PaymentStatus = "Pending";
        user.IsPaymentApproved = false;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Payment transaction ID has been submitted." });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetPaymentStatus()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User has not been found." });

        return Ok(new PaymentStatusDtos
        {
            Status = user.PaymentStatus,
            TransactionId = user.TransactionId ?? string.Empty
        });
    }
}
