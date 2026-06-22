// Hubs/MoodMapHub.cs
using GoldenWhistle.Data;
using GoldenWhistle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Hubs;

public class MoodMapHub : Hub
{
    private readonly ApplicationDbContext _db;

    public MoodMapHub(ApplicationDbContext db)
    {
        _db = db;
    }

    // Called by client to cast a vote
    // Requires the user to be logged in
    [Authorize]
    public async Task CastVote(long apiMatchId, string mood)
    {
        if (!Enum.TryParse<MoodType>(mood, ignoreCase: true, out var moodType))
        {
            throw new HubException("Invalid mood value. Use Ecstasy, Agony, or Anxiety.");
        }

        var userId = Context.UserIdentifier;

        var match = await _db.Matches.FirstOrDefaultAsync(m => m.ApiMatchId == apiMatchId);
        if (match is null)
        {
            throw new HubException("Match not found.");
        }

        // One vote per user per match — upsert
        var existing = await _db.MoodVotes
            .FirstOrDefaultAsync(v => v.MatchId == match.Id && v.UserId == userId);

        if (existing is null)
        {
            _db.MoodVotes.Add(new MoodVote
            {
                MatchId = match.Id,
                UserId = userId!,
                Mood = moodType,
                VotedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Mood = moodType;
            existing.VotedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Broadcast updated tallies to everyone watching this match
        await BroadcastTalliesAsync(match.Id, apiMatchId);
    }

    // Called by client on page load to get current tallies for a match
    public async Task RequestTallies(long apiMatchId)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.ApiMatchId == apiMatchId);
        if (match is null) return;

        await BroadcastTalliesAsync(match.Id, apiMatchId);
    }

    private async Task BroadcastTalliesAsync(int matchId, long apiMatchId)
    {
        var votes = await _db.MoodVotes
            .Where(v => v.MatchId == matchId)
            .ToListAsync();

        var payload = new
        {
            apiMatchId,
            ecstasy = votes.Count(v => v.Mood == MoodType.Ecstasy),
            agony = votes.Count(v => v.Mood == MoodType.Agony),
            anxiety = votes.Count(v => v.Mood == MoodType.Anxiety),
            total = votes.Count
        };

        await Clients.All.SendAsync("ReceiveTallies", payload);
    }
}