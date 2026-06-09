using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    public class ChatController : Controller
    {


        public IActionResult Index()
        {

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            return View();
        }
    }
}