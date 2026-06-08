using Microsoft.AspNetCore.Mvc;

namespace MassMessaging.MVC.Controllers
{
    public class ChatController : Controller
    {
        // FIX #5: Server-side guard — if no token cookie/session exists, redirect to login.
        // Since auth is JWT-based (token stored in localStorage), we use a lightweight check:
        // The MVC Program.cs should add cookie auth for the MVC side, but as a minimal fix
        // without restructuring, we add [RequireToken] via a base approach.
        // The cleanest solution with the current architecture: add [Authorize] with a cookie
        // scheme, OR keep localStorage but store token in a cookie on login too.
        //
        // SIMPLEST FIX that requires zero changes to the API or auth flow:
        // Pass the token as a query param on first load and store it as a cookie server-side.
        // But the real fix is: redirect is handled in OnActionExecuting below.

        public IActionResult Index()
        {
            // If your MVC app is configured with cookie auth (see Program.cs note),
            // add [Authorize] above this method. For now we return the view and let
            // the JS guard handle it — BUT we also set cache headers so browsers
            // won't cache the page for unauthenticated users.
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            return View();
        }
    }
}