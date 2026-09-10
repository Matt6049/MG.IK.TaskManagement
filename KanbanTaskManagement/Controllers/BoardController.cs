using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Repositories;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Controllers;

[Authorize]
public class BoardController : Controller {
	private readonly MongoDBContext _db;
	private readonly ICurrentUser _currentUser;
	private readonly IPermissionService _permissions;
	private readonly BoardRepository repository;

	public BoardController(MongoDBContext db, ICurrentUser currentUser, IPermissionService permissions) {
		_db = db;
		_currentUser = currentUser;
		_permissions = permissions;
		repository = new() {
			Collection = _db.BoardCollection,
			Database = _db
		};
	}

	[HttpGet]
	public async Task<IActionResult> Index(string id) {
		if (string.IsNullOrEmpty(id))
			return RedirectToAction("Index", "Dashboard");

		var queryRes = await repository.GetById(id);
		if (!queryRes.Ok)
			return queryRes.ErrorStatus;

		var board = queryRes.Result!;
		var access = await ResolveAccess(board);
		if (!access.CanView)
			return NotFound();

		var vm = new BoardDetailsViewModel { Board = board, Access = access };
		if (access.CanEditTasks)
			vm.Assignable = await LoadAssignableUsernames(board);
		if (access.CanManageMembers)
			vm.Members = await LoadMembers(board);
		if (access.CanManageBoard) {
			vm.Groups = await LoadManagedGroups();
			if (board.GroupId != ObjectId.Empty)
				vm.LinkedGroupName = (await _db.GroupCollection.Find(g => g.Id == board.GroupId).FirstOrDefaultAsync())?.Name;
		}
		return View(vm);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateBoard(string boardName) {
		if (string.IsNullOrWhiteSpace(boardName))
			return RedirectToAction("Index", "Dashboard");
		if (_currentUser.UserId is not { } ownerId)
			return Forbid();

		Board board = new() {
			Name = boardName.Trim(),
			OwnerId = ownerId,
			OwnerName = _currentUser.Username ?? "",
			IsUserOwned = true,
		};
		await repository.InsertBoard(board);
		return RedirectToAction("Index", new { id = board.Id.ToString() });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateTask(string boardId, int columnType, string taskName, string taskDescription, DateTime? startDate, DateTime? dueDate, int priority, string[]? assignedUsers) {
		var (board, error) = await LoadForEdit(boardId);
		if (error is not null)
			return error;
		if (string.IsNullOrWhiteSpace(taskName))
			return RedirectToAction("Index", new { id = boardId });

		KanbanTask task = new() {
			CreatorName = _currentUser.Username ?? "",
			Name = taskName.Trim(),
			Description = taskDescription,
			StartDate = AsUtcDate(startDate),
			DueDate = AsUtcDate(dueDate),
			Priority = ClampPriority(priority),
			AssignedUsers = await SanitizeAssignees(board!, assignedUsers),
		};
		var res = await repository.InsertTask(board!, (ColumnType) columnType, task);
		if (!res.Ok)
			return res.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateTask(string boardId, string taskId, string newName, string newDescription, DateTime? startDate, DateTime? dueDate, int priority, string[]? assignedUsers) {
		var (board, error) = await LoadForEdit(boardId);
		if (error is not null)
			return error;

		var taskRes = await repository.GetTaskById(boardId, taskId);
		if (!taskRes.Ok)
			return taskRes.ErrorStatus;

		KanbanTask task = taskRes.Result!;
		task.Name = newName;
		task.Description = newDescription;
		task.StartDate = AsUtcDate(startDate);
		task.DueDate = AsUtcDate(dueDate);
		task.Priority = ClampPriority(priority);
		task.AssignedUsers = await SanitizeAssignees(board!, assignedUsers);

		var updateRes = await repository.UpdateTask(boardId, task);
		if (!updateRes.Ok)
			return updateRes.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> MoveTask(string boardId, string taskId, int targetColumn) {
		var (_, error) = await LoadForEdit(boardId);
		if (error is not null)
			return error;

		var taskRes = await repository.GetTaskById(boardId, taskId);
		if (!taskRes.Ok)
			return taskRes.ErrorStatus;

		var target = (ColumnType) targetColumn;
		KanbanTask task = taskRes.Result!;
		task.CompletedAt = target == ColumnType.DONE ? DateTime.UtcNow : null;

		var res = await repository.MoveTask(boardId, taskId, target, task);
		if (!res.Ok)
			return res.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteTask(string boardId, string taskId) {
		var (_, error) = await LoadForEdit(boardId);
		if (error is not null)
			return error;

		var res = await repository.DeleteTask(boardId, taskId);
		if (!res.Ok)
			return res.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	private static DateTime? AsUtcDate(DateTime? value)
		=> value.HasValue ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc) : null;

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddMember(string boardId, string username, int role) {
		var (board, error) = await LoadForManageMembers(boardId);
		if (error is not null)
			return error;

		var user = await _db.UserCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
		if (user is null) {
			TempData["BoardError"] = $"Nie znaleziono użytkownika \"{username}\".";
			return RedirectToAction("Index", new { id = boardId });
		}
		if (user.Id == board!.OwnerId || board.Members.Any(m => m.UserId == user.Id))
			return RedirectToAction("Index", new { id = boardId });

		await repository.AddMember(board.Id, new BoardMember { UserId = user.Id, Role = ClampBoardRole(role) });
		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SetMemberRole(string boardId, string userId, int role) {
		var (board, error) = await LoadForManageMembers(boardId);
		if (error is not null)
			return error;
		if (!ObjectId.TryParse(userId, out ObjectId targetId))
			return BadRequest();

		if (board!.Members.Any(m => m.UserId == targetId))
			await repository.SetMemberRole(board.Id, targetId, ClampBoardRole(role));

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RemoveMember(string boardId, string userId) {
		var (board, error) = await LoadForManageMembers(boardId);
		if (error is not null)
			return error;
		if (!ObjectId.TryParse(userId, out ObjectId targetId))
			return BadRequest();

		await repository.RemoveMember(board!.Id, targetId);
		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SetGroup(string boardId, string groupId) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		var board = boardRes.Result!;
		var access = await ResolveAccess(board);
		if (!access.CanManageBoard)
			return Forbid();

		ObjectId target = ObjectId.Empty;
		if (!string.IsNullOrEmpty(groupId)) {
			if (!ObjectId.TryParse(groupId, out target))
				return BadRequest();
			if (!await UserManagesGroup(target))
				return Forbid();
		}

		await repository.SetGroup(board.Id, target);
		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SetTheme(string boardId, string headerFrom, string headerTo, string backlogColor, string inProgressColor, string doneColor) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		var access = await ResolveAccess(boardRes.Result!);
		if (!access.CanManageBoard)
			return Forbid();

		var current = boardRes.Result!.Theme;
		var theme = new BoardTheme {
			HeaderFrom = Hex(headerFrom, current.HeaderFrom),
			HeaderTo = Hex(headerTo, current.HeaderTo),
			BacklogColor = Hex(backlogColor, current.BacklogColor),
			InProgressColor = Hex(inProgressColor, current.InProgressColor),
			DoneColor = Hex(doneColor, current.DoneColor),
		};

		await repository.SetTheme(boardRes.Result!.Id, theme);
		return RedirectToAction("Index", new { id = boardId });
	}

	private static string Hex(string? value, string fallback)
		=> value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$")
			? value.ToLowerInvariant()
			: fallback;

	private async Task<(Board? board, IActionResult? error)> LoadForEdit(string boardId) {
		var res = await repository.GetById(boardId);
		if (!res.Ok)
			return (null, res.ErrorStatus);

		var access = await ResolveAccess(res.Result!);
		return access.CanEditTasks ? (res.Result, null) : (res.Result, Forbid());
	}

	private async Task<(Board? board, IActionResult? error)> LoadForManageMembers(string boardId) {
		var res = await repository.GetById(boardId);
		if (!res.Ok)
			return (null, res.ErrorStatus);

		var access = await ResolveAccess(res.Result!);
		return access.CanManageMembers ? (res.Result, null) : (res.Result, Forbid());
	}

	private async Task<BoardAccess> ResolveAccess(Board board) {
		if (_currentUser.UserId is not { } userId)
			return BoardAccess.None;
		return await _permissions.ResolveAsync(userId, board);
	}

	private async Task<List<BoardMemberView>> LoadMembers(Board board) {
		var ids = board.Members.Select(m => m.UserId).Append(board.OwnerId).Distinct().ToList();
		var users = await _db.UserCollection.Find(u => ids.Contains(u.Id)).ToListAsync();
		var names = users.ToDictionary(u => u.Id, u => u.Username);

		var list = new List<BoardMemberView> {
			new() {
				UserId = board.OwnerId,
				Username = names.GetValueOrDefault(board.OwnerId, board.OwnerName),
				Role = GroupRole.OWNER,
				IsOwner = true,
			},
		};
		list.AddRange(board.Members
			.OrderByDescending(m => m.Role)
			.Select(m => new BoardMemberView {
				UserId = m.UserId,
				Username = names.GetValueOrDefault(m.UserId, "?"),
				Role = m.Role,
			}));
		return list;
	}

	private async Task<List<string>> LoadAssignableUsernames(Board board) {
		var ids = board.Members.Select(m => m.UserId).Append(board.OwnerId).ToHashSet();
		if (board.GroupId != ObjectId.Empty) {
			var group = await _db.GroupCollection.Find(g => g.Id == board.GroupId).FirstOrDefaultAsync();
			if (group is not null)
				foreach (var m in group.Members)
					ids.Add(m.UserId);
		}

		var users = await _db.UserCollection.Find(u => ids.Contains(u.Id)).ToListAsync();
		return users.Select(u => u.Username).OrderBy(n => n).ToList();
	}

	private async Task<string[]> SanitizeAssignees(Board board, string[]? requested) {
		if (requested is null || requested.Length == 0)
			return Array.Empty<string>();

		var eligible = (await LoadAssignableUsernames(board)).ToHashSet();
		return requested.Where(eligible.Contains).Distinct().ToArray();
	}

	private static TaskPriority ClampPriority(int value)
		=> (TaskPriority) Math.Clamp(value, (int) TaskPriority.LOW, (int) TaskPriority.HIGH);

	private async Task<List<GroupOption>> LoadManagedGroups() {
		if (_currentUser.UserId is not { } userId)
			return [];

		var groups = await _db.GroupCollection
			.Find(Builders<Models.Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == userId))
			.ToListAsync();

		return groups
			.Where(g => g.Members.Any(m => m.UserId == userId && m.Role >= GroupRole.ADMIN))
			.Select(g => new GroupOption { Id = g.Id, Name = g.Name ?? "(bez nazwy)" })
			.ToList();
	}

	private async Task<bool> UserManagesGroup(ObjectId groupId) {
		if (_currentUser.UserId is not { } userId)
			return false;

		var group = await _db.GroupCollection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
		return group is not null
			&& group.Members.Any(m => m.UserId == userId && m.Role >= GroupRole.ADMIN);
	}

	private static GroupRole ClampBoardRole(int requested)
		=> (GroupRole) Math.Clamp(requested, (int) GroupRole.READ_ONLY, (int) GroupRole.ADMIN);
}
