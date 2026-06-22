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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<FootballApiOptions>(
    builder.Configuration.GetSection(FootballApiOptions.SectionName));

builder.Services.AddHttpClient<IFootballApiService, FootballApiService>();

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

app.Run();