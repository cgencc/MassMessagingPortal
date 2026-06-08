using Microsoft.AspNetCore.Mvc;        // View, ViewBag, Controller sınıfı için
using Microsoft.AspNetCore.Http;       // HttpContext.Session için
using MassMessaging.MVC.Services;      // AdminService namespace'i için

namespace MassMessaging.MVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            // Token'ı Session'dan al
            var token = HttpContext.Session.GetString("JWToken");

            // Eğer token yoksa Auth sayfasına at ki hata almasın
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _adminService.GetAllUsersAsync(token);
            ViewBag.Users = users;

            return View();
        }
    }
}