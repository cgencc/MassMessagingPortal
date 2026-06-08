using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}