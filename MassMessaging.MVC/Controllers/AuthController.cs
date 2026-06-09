using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{

    public class AuthController : Controller
    {
        public IActionResult Login() => View();
        public IActionResult Register() => View();
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }
    }
}