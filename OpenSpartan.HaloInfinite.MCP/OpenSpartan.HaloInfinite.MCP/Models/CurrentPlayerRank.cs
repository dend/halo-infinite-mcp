namespace OpenSpartan.Forerunner.MCP.Models
{
    public class CurrentPlayerRank
    {
        public int MaxRank { get; set; }

        public string? Title { get; set; }

        public int? CurrentRankExperience { get; set; }

        public int? RequiredRankExperience { get; set; }

        public int? ExperienceTotalRequired { get; set; }

        public int? ExperienceEarnedToDate { get; set; }
    }
}
