namespace Backend.Helpers
{
    public static class SkillMatcher
    {
        public static int CalculateScore(string? jobSkillsStr, string? userSkillsStr)
        {
            if (string.IsNullOrWhiteSpace(jobSkillsStr) || string.IsNullOrWhiteSpace(userSkillsStr))
                return 0;

            
            var requiredSkills = jobSkillsStr.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim().ToLower())
                                             .Where(s => !string.IsNullOrEmpty(s))
                                             .Distinct()
                                             .ToList();

            var userSkills = userSkillsStr.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim().ToLower())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .Distinct()
                                          .ToList();

            if (requiredSkills.Count == 0) return 100;

            int matchedCount = 0;

            foreach (var reqSkill in requiredSkills)
            {
                
                if (userSkills.Any(uSkill => uSkill.Contains(reqSkill) || reqSkill.Contains(uSkill)))
                {
                    matchedCount++;
                }
            }

            double percentage = ((double)matchedCount / requiredSkills.Count) * 100;
            return (int)Math.Round(percentage);
        }
    }
}