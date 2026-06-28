using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Services;

public class PrivateLeagueService : IPrivateLeagueService
{
    private readonly ApplicationDbContext _db;

    public PrivateLeagueService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PrivateLeague> CreateLeagueAsync(string userId, string leagueName)
    {
        var league = new PrivateLeague
        {
            Name = leagueName,
            CreatedByUserId = userId,
            JoinCode = GenerateJoinCode(),
            CreatedAt = DateTime.UtcNow
        };

        _db.PrivateLeagues.Add(league);
        await _db.SaveChangesAsync();

        _db.LeagueMembers.Add(new LeagueMember
        {
            PrivateLeagueId = league.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return league;
    }

    public async Task<PrivateLeague?> JoinLeagueAsync(string userId, string joinCode)
    {
        var league = await _db.PrivateLeagues
            .FirstOrDefaultAsync(l => l.JoinCode == joinCode.ToUpper());

        if (league is null) return null;

        bool alreadyMember = await _db.LeagueMembers
            .AnyAsync(m => m.PrivateLeagueId == league.Id && m.UserId == userId);

        if (alreadyMember) return league;

        _db.LeagueMembers.Add(new LeagueMember
        {
            PrivateLeagueId = league.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return league;
    }

    public async Task<List<LeagueMember>> GetLeaderboardAsync(int leagueId)
    {
        return await _db.LeagueMembers
            .Include(m => m.User)
            .Where(m => m.PrivateLeagueId == leagueId)
            .OrderByDescending(m => m.User.TotalPoints)
            .ToListAsync();
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}