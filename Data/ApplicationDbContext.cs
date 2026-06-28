using GoldenWhistle.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Match = GoldenWhistle.Models.Match;

namespace GoldenWhistle.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MoodVote> MoodVotes { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<BracketPick> BracketPicks { get; set; }
        public DbSet<PrivateLeague> PrivateLeagues { get; set; }
        public DbSet<LeagueMember> LeagueMembers { get; set; }
        public DbSet<MatchStats> MatchStats { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MatchStats>()
                .HasOne(s => s.Match)
                .WithOne()
                .HasForeignKey<MatchStats>(s => s.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}