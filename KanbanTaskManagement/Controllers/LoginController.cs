using Microsoft.AspNetCore.Mvc;

namespace KanbanTaskManagement.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
    }
}
