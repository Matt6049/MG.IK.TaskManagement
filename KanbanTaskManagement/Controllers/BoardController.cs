using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using System.ComponentModel;
using System.Security.Claims;


namespace KanbanTaskManagement.Controllers;
[Authorize]
public class BoardController : Controller {
	private readonly MongoDBContext _db;
	private readonly BoardRepository repository;

	public BoardController(MongoDBContext db) {
		_db = db;
		repository = new() {
			Collection = _db.BoardCollection,
			Database = _db
		};
	}

	// Zmieniamy na publiczny GET (domyślnie)
	[HttpGet]
	public async Task<IActionResult> Index(string id) {
		if (string.IsNullOrEmpty(id)) {
			return RedirectToAction("Index", "Dashboard");
		}

		// Pobieramy konkretną tablicę po jej ID
		var queryRes = await repository.GetById(id);
		if (!queryRes.Ok)
			return queryRes.ErrorStatus;
		if (queryRes.Result.OwnerName != User.FindFirst(ClaimTypes.Name).Value)
			return Unauthorized();
		return View(queryRes.Result);
	}

	[HttpPost]
	public async Task<IActionResult> CreateBoard(string boardName) {
		Board board = new() { Name = boardName, OwnerName = User.FindFirst(ClaimTypes.Name).Value, IsUserOwned = true };
		await repository.InsertBoard(board);
		return RedirectToAction("Index", board.Id);
	}

	[HttpPost]
	public async Task<IActionResult> UpdateTask(string boardId, string taskId, string newName, string newDescription) {
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;
		if (boardRes.Result.OwnerName != User.FindFirst(ClaimTypes.Name).Value)
			return Unauthorized();

		var taskRes = await repository.GetTaskById(boardId, taskId);
		if (!taskRes.Ok)
			return taskRes.ErrorStatus;
		KanbanTask task = taskRes.Result;
		task.Name = newName;
		task.Description = newDescription;

		var updateRes = await repository.UpdateTask(boardId, task);
		if (!updateRes.Ok)
			return updateRes.ErrorStatus;
		return RedirectToAction("Index", boardId);
	}

	[HttpPost]
	public async Task<IActionResult> CreateTask(string boardId, int columnType, string taskName, string taskDescription) {
		KanbanTask task = new() { CreatorName = User.FindFirstValue(ClaimTypes.Name), Name = taskName, Description = taskDescription };
		var boardRes = await repository.GetById(boardId);
		if (!boardRes.Ok)
			return boardRes.ErrorStatus;

		Board board = boardRes.Result;
		if (board.OwnerName != User.FindFirst(ClaimTypes.Name).Value)
			return Unauthorized();
		await repository.InsertTask(board, (ColumnType) columnType, task);

		return RedirectToAction("Index", boardId);
	}
}