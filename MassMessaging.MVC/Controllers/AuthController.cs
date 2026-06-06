using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    public class AuthController : Controller
    {
        // Gördüğün gibi sıfır kod, sıfır API müdahalesi. Sadece sayfayı açar.
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
    }
}