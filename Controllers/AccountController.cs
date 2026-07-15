using GoldenWhistle.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GoldenWhistle.ViewModels;

namespace GoldenWhistle.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ---- Login ----
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            // FIX (audit §5): lockoutOnFailure was false, so there was no
            // brute-force protection at all on the login form. Identity's
            // built-in lockout (see Program.cs — MaxFailedAccessAttempts /
            // DefaultLockoutTimeSpan) is now actually engaged.
            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again later.");
                return View();
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View();
        }

        // ---- Register ----
        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string firstName, string lastName, string email, string password, string? favouriteTeam)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = $"{firstName} {lastName}",
                Country = favouriteTeam,
                CreatedAt = DateTime.UtcNow,
                TotalPoints = 0
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View();
        }

        // ---- Logout ----
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ---- Google OAuth ----
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: true);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var firstName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "";
            var lastName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value ?? "";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = $"{firstName} {lastName}".Trim(),
                CreatedAt = DateTime.UtcNow,
                TotalPoints = 0
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: true);
                return RedirectToLocal(returnUrl);
            }

            return RedirectToAction("Login");
        }

        // ---- Forgot / Reset password ----
        // NEW (audit §6): Login.cshtml links to /Account/ForgotPassword,
        // which had no matching action anywhere — a guaranteed 404. This is
        // a minimal working implementation (request form + always-generic
        // confirmation, to avoid leaking which emails are registered).
        // Actually emailing the reset link requires an IEmailSender
        // implementation which wasn't part of the provided codebase — wire
        // one up (SendGrid, SES, SMTP...) and call it where noted below.
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { userId = user.Id, token }, Request.Scheme);

                    // TODO: send `resetUrl` via a real email service (IEmailSender).
                    // Intentionally not implemented here since no email
                    // provider/config was part of the reviewed codebase.
                }
            }

            // Always show the same confirmation regardless of whether the
            // email exists, to avoid leaking account existence.
            ViewData["Message"] = "If an account with that email exists, a reset link has been sent.";
            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (userId is null || token is null)
                return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
                return RedirectToAction("Login");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
                return RedirectToAction("Login");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ---- Helpers ----
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
    }

    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
