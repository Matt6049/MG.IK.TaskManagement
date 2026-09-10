using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Repositories;
using KanbanTaskManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

		ViewData["Access"] = access;
		return View(board);
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
	public async Task<IActionResult> CreateTask(string boardId, int columnType, string taskName, string taskDescription) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		Board board = boardRes.Result!;
		var access = await ResolveAccess(board);
		if (!access.CanEditTasks)
			return Forbid();

		KanbanTask task = new() {
			CreatorName = _currentUser.Username ?? "",
			Name = taskName,
			Description = taskDescription,
		};
		var res = await repository.InsertTask(board, (ColumnType) columnType, task);
		if (!res.Ok)
			return res.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateTask(string boardId, string taskId, string newName, string newDescription) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		var access = await ResolveAccess(boardRes.Result!);
		if (!access.CanEditTasks)
			return Forbid();

		var taskRes = await repository.GetTaskById(boardId, taskId);
		if (!taskRes.Ok)
			return taskRes.ErrorStatus;

		KanbanTask task = taskRes.Result!;
		task.Name = newName;
		task.Description = newDescription;

		var updateRes = await repository.UpdateTask(boardId, task);
		if (!updateRes.Ok)
			return updateRes.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteTask(string boardId, string taskId) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		var access = await ResolveAccess(boardRes.Result!);
		if (!access.CanEditTasks)
			return Forbid();

		var res = await repository.DeleteTask(boardId, taskId);
		if (!res.Ok)
			return res.ErrorStatus;

		return RedirectToAction("Index", new { id = boardId });
	}

	private async Task<BoardAccess> ResolveAccess(Board board) {
		if (_currentUser.UserId is not { } userId)
			return BoardAccess.None;
		return await _permissions.ResolveAsync(userId, board);
	}
}
