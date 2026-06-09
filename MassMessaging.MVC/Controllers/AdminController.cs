using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{

    public class AdminController : Controller
    {
        public IActionResult Index() => View();
    }
}