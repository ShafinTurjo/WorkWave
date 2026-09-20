using System.Text.RegularExpressions;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <param name="SameOwnerJobId">Id of an open job by the SAME account with the same title + company + location.</param>
/// <param name="OtherOwnerJobId">Id of a job by ANOTHER account that looks like the same job (see rule 2 below).</param>
public record DuplicateCheckResult(int? SameOwnerJobId, int? OtherOwnerJobId);

/// <summary>
/// Finds duplicate job posts. Comparisons ignore upper/lower case and extra spaces.
/// No database change is needed.
/// </summary>
public static class DuplicateJobDetector
{
    // Very short descriptions ("Sales") are not meaningful to compare word for word.
    private const int MinDescriptionLengthForExactMatch = 30;

    public static string Normalize(string? text) =>
        Regex.Replace((text ?? "").Trim().ToLowerInvariant(), @"\s+", " ");

    public static async Task<DuplicateCheckResult> CheckAsync(
        ApplicationDbContext db,
        int ownerId,
        string title,
        string company,
        string location,
        string description)
    {
        var titleKey = Normalize(title);
        var companyKey = Normalize(company);
        var locationKey = Normalize(location);
        var descriptionKey = Normalize(description);

        // Rule 1 - same account: same title + company + location while that job is still open.
        //          (A closed, rejected or removed job can be posted again.)
        var myOpenJobs = await db.Jobs
            .Where(j => j.PostedByUserId == ownerId
                        && j.IsActive
                        && j.Status != "Rejected"
                        && j.Status != "Removed")
            .Select(j => new { j.Id, j.Title, j.Company, j.Location })
            .ToListAsync();

        int? sameOwnerJobId = myOpenJobs
            .Where(j => Normalize(j.Title) == titleKey
                        && Normalize(j.Company) == companyKey
                        && Normalize(j.Location) == locationKey)
            .Select(j => (int?)j.Id)
            .FirstOrDefault();

        // Rule 2 - another account, same title, AND either
        //            a) the same company + location, or
        //            b) a word-for-word identical description.
        //          Only the small set of jobs with the same title is loaded from the database.
        var titleLower = title.Trim().ToLowerInvariant();

        var sameTitleJobs = await db.Jobs
            .Where(j => j.PostedByUserId != ownerId
                        && j.Status != "Rejected"
                        && j.Status != "Removed"
                        && j.Title.Trim().ToLower() == titleLower)
            .Select(j => new { j.Id, j.Company, j.Location, j.Description })
            .ToListAsync();

        int? otherOwnerJobId = sameTitleJobs
            .Where(j =>
                (Normalize(j.Company) == companyKey && Normalize(j.Location) == locationKey)
                || (descriptionKey.Length >= MinDescriptionLengthForExactMatch
                    && Normalize(j.Description) == descriptionKey))
            .Select(j => (int?)j.Id)
            .FirstOrDefault();

        return new DuplicateCheckResult(sameOwnerJobId, otherOwnerJobId);
    }
}
