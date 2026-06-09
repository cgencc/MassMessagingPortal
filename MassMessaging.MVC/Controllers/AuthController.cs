using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    // ✅ ARCHITECTURE: This controller only serves views.
    // All login/register logic is done by JS fetch() calls directly to the API.
    public class AuthController : Controller
    {
        public IActionResult Login() => View();
        public IActionResult Register() => View();
    }
}