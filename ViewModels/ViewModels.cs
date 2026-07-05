// ===================================================================
// ViewModels/DashboardViewModel.cs
// Used by: Views/Home/Index.cshtml
// Filled by: Dev A → HomeController.cs
// ===================================================================
namespace GoldenWhistle.ViewModels
{
    public class DashboardViewModel
    {
        public string UserDisplayName { get; set; } = string.Empty;
        public int UserTotalPoints { get; set; }
        public int UserPointsDeltaToday { get; set; }
        public int UserPredictionsMade { get; set; }
        public int UserAccuracyPct { get; set; }
        public int UserBracketRank { get; set; }
        public int TotalPlayers { get; set; }

        // Today's fixtures (from Match + Team)
        public List<FixtureCardViewModel> Fixtures { get; set; } = new();

        // Mini bracket preview
        public List<BracketMatchViewModel> BracketMatches { get; set; } = new();

        // Top 3 leaderboard (from ApplicationUser ordered by TotalPoints)
        public List<LeaderRowViewModel> TopLeaders { get; set; } = new();

        // xG chart data (from MatchStats table — M5)
        public List<XgDataPoint> XgByMatch { get; set; } = new();

        // Fan Pulse (from MoodVote counts)
        public int MoodEcstasyPct { get; set; }
        public int MoodAnxietyPct { get; set; }
        public int MoodAgonyPct { get; set; }
        public int MoodTotalVotes { get; set; }
    }

    // ===================================================================
    // ViewModels/MoodViewModel.cs
    // Used by: Views/Mood/Index.cshtml
    // Filled by: Dev A → MoodController.cs
    // ===================================================================
    public class MoodViewModel
    {
        // Current match being watched (from Match + Team)
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string MatchMinuteLabel { get; set; } = string.Empty; // e.g. "73'"
        public string ScoreLabel { get; set; } = string.Empty; // e.g. "2–1"

        // Live vote counts (calculated from MoodVote table)
        public int EcstasyPct { get; set; }
        public int AnxietyPct { get; set; }
        public int AgonyPct { get; set; }
        public int TotalVotes { get; set; }
        public int EcstasyCount { get; set; }
        public int AnxietyCount { get; set; }
        public int AgonyCount { get; set; }

        // Timeline (grouped MoodVotes by minute)
        public List<MoodTimelinePoint> Timeline { get; set; } = new();

        // Current user's vote (from MoodVote where UserId == current user)
        public string? CurrentUserVote { get; set; } // "Ecstasy" | "Anxiety" | "Agony" | null
    }

    // ===================================================================
    // ViewModels/BracketViewModel.cs
    // Used by: Views/Bracket/Index.cshtml
    // Filled by: Dev A → BracketController.cs
    // ===================================================================
    public class BracketViewModel
    {
        public int TotalCorrect { get; set; }
        public int TotalPending { get; set; }
        public string LeagueName { get; set; } = string.Empty;

        // Bracket picks (from BracketPicks + Match + Team)
        public List<BracketMatchViewModel> Picks { get; set; } = new();

        // Private league standings (from LeagueMembers + ApplicationUser)
        public List<LeagueStandingViewModel> LeagueStandings { get; set; } = new();

        // Live match events (from SignalR / MatchEvents)
        public List<LiveEventViewModel> LiveEvents { get; set; } = new();
    }

    // ===================================================================
    // ViewModels/MarketplaceViewModel.cs
    // Used by: Views/Marketplace/Index.cshtml
    // Filled by: Dev A → MarketplaceController.cs
    // ===================================================================
    public class MarketplaceViewModel
    {
        public int TotalListings { get; set; }
        public List<ListingCardViewModel> Listings { get; set; } = new();
    }

    // ===================================================================
    // ViewModels/SimulatorViewModel.cs
    // Used by: Views/Simulator/Index.cshtml
    // Filled by: Dev A → SimulatorController.cs
    // ===================================================================
    public class SimulatorViewModel
    {
        // Matches to simulate (from Match + Team, finished or upcoming)
        public List<SimMatchViewModel> Matches { get; set; } = new();
    }

    // ===================================================================
    // ViewModels/KickoffViewModel.cs
    // Used by: Views/Kickoff/Index.cshtml
    // Filled by: Dev A → KickoffController.cs
    // ===================================================================
    public class KickoffViewModel
    {
        public List<KickoffMatchViewModel> Matches { get; set; } = new();
    }

    // ===================================================================
    // Shared sub-ViewModels
    // ===================================================================

    public class FixtureCardViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamCode { get; set; } = string.Empty; // Team.ShortName
        public string AwayTeamCode { get; set; } = string.Empty;
        public string HomeTeamName { get; set; } = string.Empty; // Team.Name
        public string AwayTeamName { get; set; } = string.Empty;
        public int? HomeScore { get; set; } // Match.HomeScore
        public int? AwayScore { get; set; } // Match.AwayScore
        public string StatusBadge { get; set; } = string.Empty; // "LIVE" | "UPCOMING" | "FT"
        public string KickoffTime { get; set; } = string.Empty; // formatted from Match.KickoffUtc
        public string MatchDate { get; set; } = string.Empty;
        public bool IsLive { get; set; } // Match.Started && !Match.Finished
    }

    public class BracketMatchViewModel
    {
        public string Round { get; set; } = string.Empty; // "QF" | "SF" | "FINAL"
        public string HomeTeamCode { get; set; } = string.Empty;
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string KickoffTime { get; set; } = string.Empty;
        public bool IsLive { get; set; }
        public bool IsWinner { get; set; }
    }

    public class LeaderRowViewModel
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty; // ApplicationUser.DisplayName
        public int Points { get; set; }                  // ApplicationUser.TotalPoints
        public int PointsDelta { get; set; }
    }

    public class LeagueStandingViewModel
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int CorrectPicks { get; set; }
        public int Points { get; set; }
    }

    public class LiveEventViewModel
    {
        public int Minute { get; set; }
        public string EventType { get; set; } = string.Empty; // "GOAL" | "YELLOW" | "RED"
        public string PlayerName { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty; // "⚽" | "🟨" | "🟥"
    }

    public class MoodTimelinePoint
    {
        public string Minute { get; set; } = string.Empty;
        public int EcstasyCount { get; set; }
        public int AnxietyCount { get; set; }
        public int AgonyCount { get; set; }
    }

    public class XgDataPoint
    {
        public string MatchLabel { get; set; } = string.Empty;
        public double XgValue { get; set; }
    }

    public class ListingCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? Tag { get; set; } // "hot" | "rare" | null
        public string SellerName { get; set; } = string.Empty;
        public double SellerRating { get; set; }
        public bool IsVerified { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class SimMatchViewModel
    {
        public int MatchId { get; set; } // Match.Id
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }

    public class KickoffMatchViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public DateTime KickoffUtc { get; set; }  // Match.KickoffUtc
        public string StadiumInfo { get; set; } = string.Empty;

        // From MatchPreview table (Dev A creates this)
        public List<InjuryItemViewModel> HomeInjuries { get; set; } = new();
        public List<InjuryItemViewModel> AwayInjuries { get; set; } = new();
        public TacticViewModel? HomeTactic { get; set; }
        public TacticViewModel? AwayTactic { get; set; }
        public List<FactViewModel> Facts { get; set; } = new();
        public H2HViewModel? H2H { get; set; }
    }

    public class InjuryItemViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "out" | "doubt" | "return"
    }

    public class TacticViewModel
    {
        public string Formation { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string KeyPlayer { get; set; } = string.Empty;
        public string KeyPlayerInitial { get; set; } = string.Empty;
    }

    public class FactViewModel
    {
        public string Emoji { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // "green" | "gold" | "red" | "blue"
    }

    public class H2HViewModel
    {
        public int HomeWins { get; set; }
        public int Draws { get; set; }
        public int AwayWins { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }
}
