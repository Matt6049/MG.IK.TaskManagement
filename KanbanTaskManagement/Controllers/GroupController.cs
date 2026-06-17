using Microsoft.AspNetCore.Mvc;
using KanbanTaskManagement.Data;
using MongoDB.Driver;
using KanbanTaskManagement.Models;
using MongoDB.Bson;
using KanbanTaskManagement.ViewModels;

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

			var aggregate = _db.GroupCollection.Aggregate()
                .Match(Builders<Group>.Filter.Eq(group => group.Id, objectId))
                .Unwind<Group, GroupMember>(group => group.Members)
                .Lookup<GroupMember, User, GroupMemberLookup>(
                    foreignCollection: _db.UserCollection,
                    localField: member => member.UserId,
                    foreignField: user => user.Id,
                    @as: match => match.Matches)
                .Unwind<GroupMemberLookup, GroupMemberDTO>(lookup => lookup.Matches);
            var results = await aggregate.ToListAsync();
            return PartialView("_GroupMembers", results);
        }
    }
}