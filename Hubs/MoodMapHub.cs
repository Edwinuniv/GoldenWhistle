using Microsoft.AspNetCore.SignalR;

namespace GoldenWhistle.Hubs
{
    public class MoodMapHub : Hub
    {
        public async Task SendMoodUpdate(int matchId, string mood, int ecstasy, int agony, int anxiety)
        {
            await Clients.All.SendAsync("ReceiveMoodUpdate", matchId, mood, ecstasy, agony, anxiety);
        }
    }
}