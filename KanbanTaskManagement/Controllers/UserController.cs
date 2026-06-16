using Microsoft.AspNetCore.Mvc;

namespace KanbanTaskManagement.Controllers
{
    public class UserController : Controller
    {

        [Route("User/Profile")]
        public IActionResult Profile()
        {

            return View("~/Views/User/Profile.cshtml");
        }
    }
}