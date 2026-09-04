namespace Backend.Helpers
{
    public static class SkillMatcher
    {
        public static int CalculateScore(string? jobSkillsStr, string? userSkillsStr)
        {
            if (string.IsNullOrWhiteSpace(jobSkillsStr) || string.IsNullOrWhiteSpace(userSkillsStr))
                return 0;

            
            var requiredSkills = jobSkillsStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim().ToLower())
                                             .Where(s => !string.IsNullOrEmpty(s))
                                             .ToHashSet();

            var userSkills = userSkillsStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim().ToLower())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToHashSet();

            if (requiredSkills.Count == 0) return 100;

            
            int matchedCount = requiredSkills.Count(skill => userSkills.Contains(skill));

            
            double percentage = ((double)matchedCount / requiredSkills.Count) * 100;
            return (int)Math.Round(percentage);
        }
    }
}