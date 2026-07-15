using System.Security.Cryptography;
using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Services;

public class PrivateLeagueService : IPrivateLeagueService
{
    private readonly ApplicationDbContext _db;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxGenerationAttempts = 10;

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
            JoinCode = await GenerateUniqueJoinCodeAsync(),
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

    // FIX (audit §7): the previous version used `new Random()` (not
    // cryptographically secure — join codes are effectively access tokens
    // to a private league, so predictability matters) and never checked
    // whether the generated code already existed, so two leagues could
    // silently collide on the same code. We now use RandomNumberGenerator
    // and retry against the database until a free code is found.
    private async Task<string> GenerateUniqueJoinCodeAsync()
    {
        for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = GenerateRandomCode();
            var exists = await _db.PrivateLeagues.AnyAsync(l => l.JoinCode == code);
            if (!exists) return code;
        }

        throw new InvalidOperationException(
            $"Could not generate a unique join code after {MaxGenerationAttempts} attempts.");
    }

    private static string GenerateRandomCode()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
        {
            var index = RandomNumberGenerator.GetInt32(CodeChars.Length);
            buffer[i] = CodeChars[index];
        }
        return new string(buffer);
    }
}
