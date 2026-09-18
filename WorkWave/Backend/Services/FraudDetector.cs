namespace Backend.Services;

public static class FraudDetector
{
    private static readonly string[] SuspiciousKeywords = new[]
    {
        "telegram", "whatsapp", "registration fee", "send money",
        "advance payment", "bkash fee", "earn $500 daily", "no experience required",
        "security deposit", "contact manager at"
    };

    public static (bool IsFraud, string Reason) EvaluateJob(string title, string description)
    {
        var textToScan = $"{title} {description}".ToLower();

        foreach (var keyword in SuspiciousKeywords)
        {
            if (textToScan.Contains(keyword))
            {
                return (true, $"System detected suspicious keyword: '{keyword}'");
            }
        }

        return (false, string.Empty);
    }
}