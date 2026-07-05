using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace GoldenWhistle.Controllers
{
    [Authorize]
    public class PrivateLeagueController : Controller
    {
        private readonly IPrivateLeagueService _leagueService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrivateLeagueController(
            IPrivateLeagueService leagueService,
            UserManager<ApplicationUser> userManager)
        {
            _leagueService = leagueService;
            _userManager = userManager;
        }

        // ── Create a league ───────────────────────────────────────
        [HttpPost]
        [Route("api/league/create")]
        public async Task<IActionResult> Create([FromBody] CreateLeagueRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("League name is required.");

            var league = await _leagueService.CreateLeagueAsync(userId, request.Name.Trim());

            var joinLink = $"{Request.Scheme}://{Request.Host}/league/join/{league.JoinCode}";

            return Ok(new
            {
                league.Id,
                league.Name,
                league.JoinCode,
                joinLink,
                qrCodeBase64 = GenerateQrCode(joinLink)
            });
        }

        // ── Join via code ─────────────────────────────────────────
        [HttpPost]
        [Route("api/league/join")]
        public async Task<IActionResult> Join([FromBody] JoinLeagueRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.JoinCode))
                return BadRequest("Join code is required.");

            var league = await _leagueService.JoinLeagueAsync(userId, request.JoinCode.Trim());
            if (league is null)
                return NotFound("No league found with that code.");

            return Ok(new { league.Id, league.Name, league.JoinCode });
        }

        // ── Join via link ─────────────────────────────────────────
        [HttpGet]
        [Route("league/join/{code}")]
        public async Task<IActionResult> JoinViaLink(string code)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = $"/league/join/{code}" });

            var league = await _leagueService.JoinLeagueAsync(userId, code);
            if (league is null)
                return NotFound("Invalid join link.");

            return RedirectToAction("Index", "Bracket");
        }

        // ── League leaderboard ────────────────────────────────────
        [HttpGet]
        [Route("api/league/{id}/leaderboard")]
        public async Task<IActionResult> Leaderboard(int id)
        {
            var members = await _leagueService.GetLeaderboardAsync(id);

            var result = members.Select((m, i) => new
            {
                Rank = i + 1,
                UserName = m.User.DisplayName ?? m.User.UserName ?? "Fan",
                Points = m.User.TotalPoints,
                Country = m.User.Country,
                JoinedAt = m.JoinedAt
            });

            return Ok(result);
        }

        // ── Get invite info (QR + link) for an existing league ────
        [HttpGet]
        [Route("api/league/{id}/invite")]
        public async Task<IActionResult> GetInvite(int id)
        {
            var members = await _leagueService.GetLeaderboardAsync(id);
            if (members.Count == 0) return NotFound();

            var league = members.First().League;
            var joinLink = $"{Request.Scheme}://{Request.Host}/league/join/{league.JoinCode}";

            return Ok(new
            {
                league.JoinCode,
                joinLink,
                qrCodeBase64 = GenerateQrCode(joinLink)
            });
        }

        // ── QR code generator ─────────────────────────────────────
        private static string GenerateQrCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var bytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(bytes);
        }
    }
}