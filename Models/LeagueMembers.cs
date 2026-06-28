using GoldenWhistle.Models;

public class LeagueMember
{
    public int Id { get; set; }
    public int PrivateLeagueId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public PrivateLeague League { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}