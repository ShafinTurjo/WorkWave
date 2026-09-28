using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Backend.Helpers;

public static class YouTubeHelper
{
    private static readonly Regex VideoIdPattern = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the 11-character video id from the common YouTube URL shapes
    /// (watch?v=, youtu.be/, /embed/, /shorts/, /live/). Returns null if it isn't a YouTube video link.
    /// </summary>
    public static string? ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return null;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];

        string? id = null;

        if (host == "youtu.be")
        {
            id = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        else if (host is "youtube.com" or "m.youtube.com" or "music.youtube.com" or "youtube-nocookie.com")
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 1 && segments[0] == "watch")
            {
                id = QueryHelpers.ParseQuery(uri.Query).TryGetValue("v", out var v) ? v.ToString() : null;
            }
            else if (segments.Length >= 2 && segments[0] is "embed" or "shorts" or "live" or "v")
            {
                id = segments[1];
            }
        }

        return id is not null && VideoIdPattern.IsMatch(id) ? id : null;
    }
}
