using Microsoft.AspNetCore.Mvc;
using GoldenWhistle.ViewModels;
namespace GoldenWhistle.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }
    }
}