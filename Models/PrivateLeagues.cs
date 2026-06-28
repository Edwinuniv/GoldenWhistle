using GoldenWhistle.Models;

public class PrivateLeague
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;  
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<LeagueMember> Members { get; set; } = new List<LeagueMember>();
}