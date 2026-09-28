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
            return BadRequest(new { message = "Transaction ID deya baddhyatamulok." });
        }

        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User pawa jayni." });

        if (user.PaymentStatus == "Approved")
        {
            return BadRequest(new { message = "Apnar payment agei approve hoye gechhe." });
        }

        // Same trxId keu duibar use korte parbe na
        var alreadyUsed = await _db.Users.AnyAsync(u => u.Id != user.Id && u.TransactionId == trxId);
        if (alreadyUsed)
        {
            return Conflict(new { message = "Ei Transaction ID agei onno account e use kora hoyeche." });
        }

        user.TransactionId = trxId;
        user.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "bKash" : dto.PaymentMethod.Trim();
        user.PaymentStatus = "Pending";
        user.IsPaymentApproved = false;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Payment transaction ID submit hoyeche." });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetPaymentStatus()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound(new { message = "User pawa jayni." });

        return Ok(new PaymentStatusDtos
        {
            Status = user.PaymentStatus,
            TransactionId = user.TransactionId ?? string.Empty
        });
    }
}
