using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text.RegularExpressions;
using Group = KanbanTaskManagement.Models.Group;

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
            var groups = _db.GroupCollection.Aggregate()
                .Project(group => new GroupDTO { Id = group.Id, CreatedAt = group.CreatedAt, Name = group.Name })
                .ToList();
            return View("Index", groups);
        }

        public async Task<IActionResult> Create(string name) {
            Group newGroup = new() {
                Name = name
            };
            await _db.GroupCollection.InsertOneAsync(newGroup);
            return RedirectToAction("Index");
        }
		

		public async Task<IActionResult> GetGroupMembers(string id) {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return BadRequest("Incorrect Group Id format!");

            var aggregate = _db.GroupCollection.AsQueryable()
                .Where(group => group.Id == objectId)
                .SelectMany(group => group.Members)
                .Join(
                    inner: _db.UserCollection.AsQueryable(),
                    outerKeySelector: member => member.UserId,
                    innerKeySelector: user => user.Id,
                    resultSelector: (member, user) => new GroupMemberDTO() {
                        Id = user.Id,
                        Role = member.Role,
                        LastActive = user.LastActive,
                        Username = user.Username
                    });
            var results = await aggregate.ToListAsync();

            return PartialView("_GroupMembers", results);
        }
    }
}