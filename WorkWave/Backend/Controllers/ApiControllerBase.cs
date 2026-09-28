using System.Security.Claims;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

/// <summary>
/// All authenticated identity must come from the JWT (ClaimsPrincipal), never from
/// a client-supplied query/body parameter — otherwise anyone can act as any user
/// by simply changing an id in the request.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No NameIdentifier claim on the current user."));

    protected string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    protected bool IsAdmin => CurrentRole == "Admin";

    /// <summary>Returns Forbid() unless the caller is the given user or an Admin.</summary>
    protected ActionResult? EnsureSelfOrAdmin(int userId) =>
        (userId == CurrentUserId || IsAdmin) ? null : Forbid();

    /// <summary>Admins always pass; everyone else needs an approved payment.</summary>
    protected async Task<bool> HasApprovedPaymentAsync(ApplicationDbContext db) =>
        IsAdmin || await db.Users.AnyAsync(u => u.Id == CurrentUserId && u.IsPaymentApproved);

    protected ObjectResult PaymentRequired() =>
        StatusCode(StatusCodes.Status403Forbidden, new
        {
            code = "PaymentRequired",
            message = "Ei feature use korte apnar payment approve howa dorkar."
        });
}
