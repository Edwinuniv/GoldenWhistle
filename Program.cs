using GoldenWhistle.Data;
using GoldenWhistle.Hubs;
using GoldenWhistle.Models;
using GoldenWhistle.Models.Configuration;
using GoldenWhistle.Services;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// FIX (audit §5): the previous AddIdentity() call relied entirely on
// Identity's built-in defaults for lockout, which ARE reasonable
// (5 failed attempts, 5 minute lockout) — but AccountController.Login was
// calling PasswordSignInAsync with lockoutOnFailure: false, which meant
// lockout was never actually engaged regardless of these settings (fixed in
// AccountController.cs). Made the settings explicit here too so they're not
// silently relying on defaults nobody can see.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<FootballApiOptions>(
    builder.Configuration.GetSection(FootballApiOptions.SectionName));

// ===== GOOGLE AUTHENTICATION =====
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        });
}
// =================================

// ===== CUSTOM SERVICE REGISTRATIONS =====
builder.Services.AddScoped<IBracketScoringService, BracketScoringService>();
builder.Services.AddScoped<IPrivateLeagueService, PrivateLeagueService>();
builder.Services.AddScoped<IChatService, GeminiChatService>();

// ===== HTTP CLIENT REGISTRATIONS =====
// FIX: AddHttpClient<IFootballApiService, FootballApiService>() was
// previously registered TWICE (once here, once again further down in the
// original file). Harmless in practice (the factory de-dupes named
// clients) but confusing — kept once.
builder.Services.AddHttpClient<IFootballApiService, FootballApiService>();
builder.Services.AddHttpClient<IMatchStatsService, MatchStatsService>();
// =====================================

// ===== BACKGROUND SYNC =====
builder.Services.AddHostedService<GoldenWhistle.BackgroundServices.SyncBackgroundService>();
// ===========================

// ===== CONTROLLERS & SIGNALR =====
// FIX: the previous file called BOTH AddControllers() AND
// AddControllersWithViews(). The latter is a superset (adds MVC views, Razor
// Pages support, etc. on top of AddControllers), so only one is needed here.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<MoodMapHub>("/hubs/moodmap");

// NEW (audit §2): site.js connects to '/hubs/leaderboard' on the Dashboard
// and Bracket pages, but this hub was never mapped anywhere — the
// connection failed silently every time. See Hubs/LeaderboardHub.cs.
app.MapHub<LeaderboardHub>("/hubs/leaderboard");

app.Run();

// ===========================================================================
// FOLLOW-UP — NOT fully addressed by this change set, needs a decision from
// you (see also the chat answer about API cost vs fake data):
//
// 1. SyncController now requires a "SyncApi:Key" configuration value and an
//    "X-Sync-Key" request header to match it (see Controllers/SyncController.cs).
//    Add to appsettings.json (or better, environment variables / a secrets
//    manager — don't commit a real key to source control):
//        "SyncApi": { "Key": "<a long random string>" }
//    And update SyncBackgroundService (not shown in the reviewed files) to
//    either call the services directly in-process (bypassing HTTP entirely,
//    which is simpler and avoids needing the key at all), or to send that
//    header if it hits the HTTP endpoints.
//
// 2. Match.Stage is a new field (Models/Match.cs) — you'll need an EF Core
//    migration (`dotnet ef migrations add AddMatchStage`) before this will
//    run against your database.
//
// 3. Notification "read" state is still not persisted (see
//    GoldenWhistle_Audit.md §7) — NotificationsController still builds
//    ephemeral notification objects on every call. Fixing that properly
//    needs a real Notification table + migration, which wasn't in scope for
//    this pass; flagged here as the next architectural piece to tackle.
// ===========================================================================
