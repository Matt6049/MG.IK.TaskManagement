using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Group = KanbanTaskManagement.Models.Group;

namespace KanbanTaskManagement.Controllers;

[Authorize]
public class GroupController : Controller {
	private readonly MongoDBContext _db;
	private readonly ICurrentUser _currentUser;

	public GroupController(MongoDBContext db, ICurrentUser currentUser) {
		_db = db;
		_currentUser = currentUser;
	}

	public async Task<IActionResult> Index() {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var groups = await _db.GroupCollection
			.Find(Builders<Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == userId))
			.Project(g => new GroupDTO { Id = g.Id, CreatedAt = g.CreatedAt, Name = g.Name })
			.ToListAsync();

		return View("Index", groups);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(string name) {
		if (_currentUser.UserId is not { } userId)
			return Forbid();
		if (string.IsNullOrWhiteSpace(name))
			return RedirectToAction("Index");

		Group group = new() {
			Name = name.Trim(),
			Members = [new GroupMember { UserId = userId, Role = GroupRole.OWNER }],
		};
		await _db.GroupCollection.InsertOneAsync(group);
		return RedirectToAction("Index");
	}

	public async Task<IActionResult> GetGroupMembers(string id) {
		if (!ObjectId.TryParse(id, out ObjectId groupId))
			return BadRequest("Incorrect Group Id format!");
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var group = await _db.GroupCollection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
		if (group is null)
			return NotFound();

		var myRole = group.RoleOf(userId);
		if (myRole is null)
			return Forbid();

		var memberIds = group.Members.Select(m => m.UserId).ToList();
		var users = await _db.UserCollection.Find(u => memberIds.Contains(u.Id)).ToListAsync();

		var members = group.Members
			.Join(users, m => m.UserId, u => u.Id, (m, u) => new GroupMemberDTO {
				Id = u.Id,
				Username = u.Username,
				Role = m.Role,
				LastActive = u.LastActive,
			})
			.OrderByDescending(x => x.Role)
			.ThenBy(x => x.Username)
			.ToList();

		return PartialView("_GroupMembers", new GroupMembersViewModel {
			GroupId = groupId,
			CanManage = myRole >= GroupRole.ADMIN,
			Members = members,
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddMember(string groupId, string username, int role) {
		var (group, myRole, error) = await LoadForManage(groupId);
		if (error is not null)
			return error;

		var user = await _db.UserCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
		if (user is null) {
			TempData["GroupError"] = $"Nie znaleziono użytkownika \"{username}\".";
			return RedirectToAction("Index");
		}
		if (group!.Members.Any(m => m.UserId == user.Id))
			return RedirectToAction("Index");

		await _db.GroupCollection.UpdateOneAsync(
			g => g.Id == group.Id,
			Builders<Group>.Update.Push(g => g.Members, new GroupMember {
				UserId = user.Id,
				Role = ClampRole(role, myRole!.Value),
			}));

		return RedirectToAction("Index");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SetRole(string groupId, string userId, int role) {
		var (group, myRole, error) = await LoadForManage(groupId);
		if (error is not null)
			return error;
		if (!ObjectId.TryParse(userId, out ObjectId targetId))
			return BadRequest();

		var target = group!.Members.FirstOrDefault(m => m.UserId == targetId);
		if (target is null)
			return RedirectToAction("Index");
		if (target.Role == GroupRole.OWNER && myRole < GroupRole.OWNER)
			return Forbid();

		var newRole = ClampRole(role, myRole!.Value);
		if (target.Role == GroupRole.OWNER && newRole != GroupRole.OWNER && OwnerCount(group) <= 1)
			return RedirectToAction("Index");

		await _db.GroupCollection.UpdateOneAsync(
			Builders<Group>.Filter.Eq(g => g.Id, group.Id)
				& Builders<Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == targetId),
			Builders<Group>.Update.Set("Members.$.Role", (int) newRole));

		return RedirectToAction("Index");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RemoveMember(string groupId, string userId) {
		var (group, myRole, error) = await LoadForManage(groupId);
		if (error is not null)
			return error;
		if (!ObjectId.TryParse(userId, out ObjectId targetId))
			return BadRequest();

		var target = group!.Members.FirstOrDefault(m => m.UserId == targetId);
		if (target is null)
			return RedirectToAction("Index");
		if (target.Role == GroupRole.OWNER && (myRole < GroupRole.OWNER || OwnerCount(group) <= 1))
			return RedirectToAction("Index");

		await _db.GroupCollection.UpdateOneAsync(
			g => g.Id == group.Id,
			Builders<Group>.Update.PullFilter(g => g.Members, m => m.UserId == targetId));

		return RedirectToAction("Index");
	}

	private async Task<(Group? group, GroupRole? myRole, IActionResult? error)> LoadForManage(string groupId) {
		if (!ObjectId.TryParse(groupId, out ObjectId gid))
			return (null, null, BadRequest());
		if (_currentUser.UserId is not { } userId)
			return (null, null, Forbid());

		var group = await _db.GroupCollection.Find(g => g.Id == gid).FirstOrDefaultAsync();
		if (group is null)
			return (null, null, NotFound());

		var myRole = group.RoleOf(userId);
		if (myRole is null || myRole < GroupRole.ADMIN)
			return (group, myRole, Forbid());

		return (group, myRole, null);
	}

	private static int OwnerCount(Group group)
		=> group.Members.Count(m => m.Role == GroupRole.OWNER);

	private static GroupRole ClampRole(int requested, GroupRole actorRole) {
		var value = (GroupRole) Math.Clamp(requested, (int) GroupRole.READ_ONLY, (int) GroupRole.OWNER);
		var max = actorRole >= GroupRole.OWNER ? GroupRole.OWNER : GroupRole.ADMIN;
		return value > max ? max : value;
	}
}
