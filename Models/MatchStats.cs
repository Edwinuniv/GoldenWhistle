namespace GoldenWhistle.Models
{
    public class MatchStats
    {
        public int Id { get; set; }
        public int MatchId { get; set; }

        // ── Goal scorers ──────────────────────────────────────────
        public string? FirstScorerName { get; set; }
        public string? LastScorerName { get; set; }
        public string GoalScorerNamesRaw { get; set; } = string.Empty;
        public string OwnGoalScorerNamesRaw { get; set; } = string.Empty;

        // ── Assists ───────────────────────────────────────────────
        public string? MostAssistsPlayerName { get; set; }
        public int? MostAssistsCount { get; set; }

        // ── Man of the Match ──────────────────────────────────────
        public string? ManOfTheMatchName { get; set; }
        public long? ManOfTheMatchTeamId { get; set; }

        // ── Discipline ───────────────────────────────────────────
        public long? MostYellowsTeamId { get; set; }
        public int? HomeYellowCards { get; set; }
        public int? AwayYellowCards { get; set; }
        public long? MostRedsTeamId { get; set; }
        public int? HomeRedCards { get; set; }
        public int? AwayRedCards { get; set; }

        // ── Fouls ────────────────────────────────────────────────
        public long? MostFoulsTeamId { get; set; }
        public string? MostFoulsPlayerName { get; set; }
        public int? HomeFouls { get; set; }
        public int? AwayFouls { get; set; }

        // ── Set pieces ───────────────────────────────────────────
        public long? MostCornersTeamId { get; set; }
        public int? HomeCorners { get; set; }
        public int? AwayCorners { get; set; }
        public int? HomeFreeKicks { get; set; }
        public int? AwayFreeKicks { get; set; }
        public int? HomePenalties { get; set; }
        public int? AwayPenalties { get; set; }

        // ── Possession & passing ─────────────────────────────────
        public long? BetterPossessionTeamId { get; set; }
        public double? HomePossessionPct { get; set; }
        public double? AwayPossessionPct { get; set; }
        public long? MostPassesTeamId { get; set; }
        public string? MostPassesPlayerName { get; set; }
        public int? HomePasses { get; set; }
        public int? AwayPasses { get; set; }
        public double? HomePassAccuracyPct { get; set; }
        public double? AwayPassAccuracyPct { get; set; }

        // ── Shots ────────────────────────────────────────────────
        public int? HomeShotsTotal { get; set; }
        public int? AwayShotsTotal { get; set; }
        public int? HomeShotsOnTarget { get; set; }
        public int? AwayShotsOnTarget { get; set; }

        // ── Expected goals ───────────────────────────────────────
        public long? HigherXgTeamId { get; set; }
        public double? HomeXg { get; set; }
        public double? AwayXg { get; set; }

        // ── Goalkeeper saves ─────────────────────────────────────
        public string? MostSavesGoalkeeperName { get; set; }
        public long? MostSavesTeamId { get; set; }
        public int? HomeSaves { get; set; }
        public int? AwaySaves { get; set; }

        // ── Duels & pressure ─────────────────────────────────────
        public int? HomeDuelsWon { get; set; }
        public int? AwayDuelsWon { get; set; }
        public int? HomeAerialDuelsWon { get; set; }
        public int? AwayAerialDuelsWon { get; set; }
        public int? HomeTackles { get; set; }
        public int? AwayTackles { get; set; }
        public int? HomeInterceptions { get; set; }
        public int? AwayInterceptions { get; set; }

        // ── Offsides ─────────────────────────────────────────────
        public int? HomeOffsides { get; set; }
        public int? AwayOffsides { get; set; }

        // ── Distance covered ─────────────────────────────────────
        public string? MostDistancePlayerName { get; set; }
        public double? HomeDistanceCoveredKm { get; set; }
        public double? AwayDistanceCoveredKm { get; set; }

        // ── Meta ─────────────────────────────────────────────────
        public DateTime? FetchedAt { get; set; }
        public bool IsComplete { get; set; }

        // ── Navigation ───────────────────────────────────────────
        public Match Match { get; set; } = null!;

        // ── Computed helpers (not stored in DB) ───────────────────
        public List<string> GoalScorerNames =>
            string.IsNullOrEmpty(GoalScorerNamesRaw)
                ? new List<string>()
                : GoalScorerNamesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim()).ToList();

        public List<string> OwnGoalScorerNames =>
            string.IsNullOrEmpty(OwnGoalScorerNamesRaw)
                ? new List<string>()
                : OwnGoalScorerNamesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim()).ToList();
    }
}