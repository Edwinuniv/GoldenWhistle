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

        // ─── EXISTING DbSets ──────────────────────────────────────
        public DbSet<MoodVote> MoodVotes { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<BracketPick> BracketPicks { get; set; }
        public DbSet<PrivateLeague> PrivateLeagues { get; set; }
        public DbSet<LeagueMember> LeagueMembers { get; set; }
        public DbSet<MatchStats> MatchStats { get; set; }

        // ─── NEW DbSets ──────────────────────────────────────────
        public DbSet<JerseyListing> JerseyListings { get; set; }
        public DbSet<PubLocation> PubLocations { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ─── EXISTING CONFIGURATIONS ──────────────────────────
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

            // ─── NEW CONFIGURATIONS ──────────────────────────────

            // JerseyListing → User (Seller)
            builder.Entity<JerseyListing>()
                .HasOne(j => j.Seller)
                .WithMany()
                .HasForeignKey(j => j.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message → User (Sender)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message → JerseyListing
            builder.Entity<Message>()
                .HasOne(m => m.Listing)
                .WithMany()
                .HasForeignKey(m => m.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}