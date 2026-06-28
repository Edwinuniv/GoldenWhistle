using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldenWhistle.Migrations
{
    /// <inheritdoc />
    public partial class AddBracketAndLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BracketPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    PredictedOutcome = table.Column<int>(type: "int", nullable: false),
                    PredictedHomeScore = table.Column<int>(type: "int", nullable: true),
                    PredictedAwayScore = table.Column<int>(type: "int", nullable: true),
                    PredictedFirstScorerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedAnytimeScorerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedLastScorerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedOwnGoal = table.Column<bool>(type: "bit", nullable: false),
                    PredictedOwnGoalTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostAssistsPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedManOfTheMatchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedMostYellowsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostRedsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostFoulsPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedMostFoulsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostCornersTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedBetterPossessionTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostPassesTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostPassesPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedHigherXgTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostSavesGoalkeeperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PredictedMostSavesTeamId = table.Column<long>(type: "bigint", nullable: true),
                    PredictedMostDistancePlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    IsScored = table.Column<bool>(type: "bit", nullable: false),
                    IsUpset = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketPicks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BracketPicks_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    FirstScorerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastScorerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoalScorerNamesRaw = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnGoalScorerNamesRaw = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MostAssistsPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MostAssistsCount = table.Column<int>(type: "int", nullable: true),
                    ManOfTheMatchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManOfTheMatchTeamId = table.Column<long>(type: "bigint", nullable: true),
                    MostYellowsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomeYellowCards = table.Column<int>(type: "int", nullable: true),
                    AwayYellowCards = table.Column<int>(type: "int", nullable: true),
                    MostRedsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomeRedCards = table.Column<int>(type: "int", nullable: true),
                    AwayRedCards = table.Column<int>(type: "int", nullable: true),
                    MostFoulsTeamId = table.Column<long>(type: "bigint", nullable: true),
                    MostFoulsPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeFouls = table.Column<int>(type: "int", nullable: true),
                    AwayFouls = table.Column<int>(type: "int", nullable: true),
                    MostCornersTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomeCorners = table.Column<int>(type: "int", nullable: true),
                    AwayCorners = table.Column<int>(type: "int", nullable: true),
                    HomeFreeKicks = table.Column<int>(type: "int", nullable: true),
                    AwayFreeKicks = table.Column<int>(type: "int", nullable: true),
                    HomePenalties = table.Column<int>(type: "int", nullable: true),
                    AwayPenalties = table.Column<int>(type: "int", nullable: true),
                    BetterPossessionTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomePossessionPct = table.Column<double>(type: "float", nullable: true),
                    AwayPossessionPct = table.Column<double>(type: "float", nullable: true),
                    MostPassesTeamId = table.Column<long>(type: "bigint", nullable: true),
                    MostPassesPlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomePasses = table.Column<int>(type: "int", nullable: true),
                    AwayPasses = table.Column<int>(type: "int", nullable: true),
                    HomePassAccuracyPct = table.Column<double>(type: "float", nullable: true),
                    AwayPassAccuracyPct = table.Column<double>(type: "float", nullable: true),
                    HomeShotsTotal = table.Column<int>(type: "int", nullable: true),
                    AwayShotsTotal = table.Column<int>(type: "int", nullable: true),
                    HomeShotsOnTarget = table.Column<int>(type: "int", nullable: true),
                    AwayShotsOnTarget = table.Column<int>(type: "int", nullable: true),
                    HigherXgTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomeXg = table.Column<double>(type: "float", nullable: true),
                    AwayXg = table.Column<double>(type: "float", nullable: true),
                    MostSavesGoalkeeperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MostSavesTeamId = table.Column<long>(type: "bigint", nullable: true),
                    HomeSaves = table.Column<int>(type: "int", nullable: true),
                    AwaySaves = table.Column<int>(type: "int", nullable: true),
                    HomeDuelsWon = table.Column<int>(type: "int", nullable: true),
                    AwayDuelsWon = table.Column<int>(type: "int", nullable: true),
                    HomeAerialDuelsWon = table.Column<int>(type: "int", nullable: true),
                    AwayAerialDuelsWon = table.Column<int>(type: "int", nullable: true),
                    HomeTackles = table.Column<int>(type: "int", nullable: true),
                    AwayTackles = table.Column<int>(type: "int", nullable: true),
                    HomeInterceptions = table.Column<int>(type: "int", nullable: true),
                    AwayInterceptions = table.Column<int>(type: "int", nullable: true),
                    HomeOffsides = table.Column<int>(type: "int", nullable: true),
                    AwayOffsides = table.Column<int>(type: "int", nullable: true),
                    MostDistancePlayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeDistanceCoveredKm = table.Column<double>(type: "float", nullable: true),
                    AwayDistanceCoveredKm = table.Column<double>(type: "float", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrivateLeagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateLeagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateLeagues_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeagueMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrivateLeagueId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueMembers_PrivateLeagues_PrivateLeagueId",
                        column: x => x.PrivateLeagueId,
                        principalTable: "PrivateLeagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BracketPicks_MatchId",
                table: "BracketPicks",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketPicks_UserId",
                table: "BracketPicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMembers_PrivateLeagueId",
                table: "LeagueMembers",
                column: "PrivateLeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMembers_UserId",
                table: "LeagueMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStats_MatchId",
                table: "MatchStats",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateLeagues_CreatedById",
                table: "PrivateLeagues",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BracketPicks");

            migrationBuilder.DropTable(
                name: "LeagueMembers");

            migrationBuilder.DropTable(
                name: "MatchStats");

            migrationBuilder.DropTable(
                name: "PrivateLeagues");
        }
    }
}
