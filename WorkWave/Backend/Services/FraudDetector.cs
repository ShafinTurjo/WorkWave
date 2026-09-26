using System.Linq;

public static class FraudDetector
{
    private static readonly string[] SuspiciousKeywords = new[] { "telegram", "whatsapp", "registration fee", "send money", "advance payment", "security deposit", "no experience required" };

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

        
        bool hasNumber = textToScan.Any(char.IsDigit);

        
        bool hasMoneyWord = textToScan.Contains("taka") || textToScan.Contains("tk") ||
                              textToScan.Contains("bdt") || textToScan.Contains("fee") ||
                              textToScan.Contains("deposit") || textToScan.Contains("advance") ||
                              textToScan.Contains("bkash") || textToScan.Contains("nagad");

        if (hasNumber && hasMoneyWord)
        {
            return (true, "System detected suspicious payment amount and demand.");
        }

        return (false, string.Empty);
    }
}