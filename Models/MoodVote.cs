namespace GoldenWhistle.Models
{
    public enum MoodType
    {
        Ecstasy,
        Agony,
        Anxiety
    }

    public class MoodVote
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public MoodType Mood { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        public Match Match { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}