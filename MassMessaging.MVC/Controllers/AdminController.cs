using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    // ✅ ARCHITECTURE: Just serves the view. The JS inside the view checks
    // localStorage for a valid Admin JWT and redirects if missing/invalid.
    public class AdminController : Controller
    {
        public IActionResult Index() => View();
    }
}