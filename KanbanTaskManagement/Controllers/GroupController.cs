using Microsoft.AspNetCore.Mvc;
using KanbanTaskManagement.Data;
using MongoDB.Driver;

namespace KanbanTaskManagement.Controllers
{
    public class GroupController : Controller
    {
        private readonly MongoDBContext _db;

        public GroupController(MongoDBContext db)
        {
            _db = db;
        }


        public IActionResult Index()
        {
            var groups = _db.GroupCollection.Find(_ => true).ToList();
            return View(groups);
        }
    }
}