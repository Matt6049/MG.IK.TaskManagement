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
public class CalendarController : Controller {
	private readonly MongoDBContext _db;
	private readonly ICurrentUser _currentUser;
	private readonly IPermissionService _permissions;
	private readonly BoardRepository _boards;

	public CalendarController(MongoDBContext db, ICurrentUser currentUser, IPermissionService permissions) {
		_db = db;
		_currentUser = currentUser;
		_permissions = permissions;
		_boards = new() {
			Collection = _db.BoardCollection,
			Database = _db
		};
	}

	[HttpGet]
	public async Task<IActionResult> Index(string? boardId, string? groupId) {
		ViewData["BoardId"] = boardId;
		ViewData["GroupId"] = groupId;

		if (_currentUser.UserId is { } userId) {
			if (!string.IsNullOrEmpty(boardId)) {
				var res = await _boards.GetById(boardId);
				if (res.Ok && (await _permissions.ResolveAsync(userId, res.Result!)).CanView)
					ViewData["ScopeName"] = res.Result!.Name;
			} else if (!string.IsNullOrEmpty(groupId) && ObjectId.TryParse(groupId, out ObjectId gid)) {
				var group = await _db.GroupCollection.Find(g => g.Id == gid).FirstOrDefaultAsync();
				if (group is not null && group.Members.Any(m => m.UserId == userId))
					ViewData["ScopeName"] = group.Name;
			}
		}

		return View();
	}

	[HttpGet]
	public async Task<IActionResult> Events(DateTime from, DateTime to, string? boardId, string? groupId) {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var boards = await ResolveBoards(userId, boardId, groupId);
		var events = new List<CalendarEventDTO>();

		foreach (var board in boards) {
			foreach (var column in board.Columns) {
				foreach (var task in column.Tasks) {
					if (task.DueDate is not { } due)
						continue;
					if (due.Date < from.Date || due.Date >= to.Date)
						continue;

					events.Add(new CalendarEventDTO {
						TaskId = task.Id.ToString(),
						BoardId = board.Id.ToString(),
						BoardName = board.Name,
						Title = task.Name,
						Date = due.ToString("yyyy-MM-dd"),
						ColumnType = column.Type.ToString(),
						Priority = task.Priority.ToString(),
						Done = task.CompletedAt is not null,
					});
				}
			}
		}

		return Json(events);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Reschedule(string boardId, string taskId, DateTime newDue) {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var boardRes = await _boards.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		var access = await _permissions.ResolveAsync(userId, boardRes.Result!);
		if (!access.CanEditTasks)
			return Forbid();

		var taskRes = await _boards.GetTaskById(boardId, taskId);
		if (!taskRes.Ok)
			return taskRes.ErrorStatus;

		var task = taskRes.Result!;
		task.DueDate = DateTime.SpecifyKind(newDue.Date, DateTimeKind.Utc);

		var res = await _boards.UpdateTask(boardId, task);
		if (!res.Ok)
			return res.ErrorStatus;

		return Ok();
	}

	private async Task<List<Board>> ResolveBoards(ObjectId userId, string? boardId, string? groupId) {
		if (!string.IsNullOrEmpty(boardId)) {
			var res = await _boards.GetById(boardId);
			if (!res.Ok)
				return [];
			var access = await _permissions.ResolveAsync(userId, res.Result!);
			return access.CanView ? [res.Result!] : [];
		}

		var accessible = await _boards.GetAccessibleBoards(userId);
		if (!string.IsNullOrEmpty(groupId) && ObjectId.TryParse(groupId, out ObjectId gid))
			accessible = accessible.Where(b => b.GroupId == gid).ToList();

		return accessible;
	}
}
