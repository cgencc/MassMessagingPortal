using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    public class ChatController : Controller
    {
        // Sıfır kod. UI sadece ekranı gösterir.
        public IActionResult Index()
        {
            return View();
        }
    }
}