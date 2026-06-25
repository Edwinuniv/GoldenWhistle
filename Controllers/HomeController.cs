using Microsoft.AspNetCore.Mvc;

namespace GoldenWhistle.Controllers
{
    public class HomeController : Controller
    {
        // Public homepage — no auth required
        public IActionResult Index()
        {
            // If already logged in, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }
    }
}