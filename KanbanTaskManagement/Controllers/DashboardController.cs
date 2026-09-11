using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Repositories;
using KanbanTaskManagement.Services;
using KanbanTaskManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanbanTaskManagement.Controllers;

[Authorize]
public class DashboardController : Controller {
	private readonly BoardRepository _boards;
	private readonly ICurrentUser _currentUser;

	public DashboardController(MongoDBContext context, ICurrentUser currentUser) {
		_boards = new() {
			Collection = context.BoardCollection,
			Database = context
		};
		_currentUser = currentUser;
	}

	public async Task<IActionResult> Index() {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var boards = await _boards.GetAccessibleBoards(userId);
		var myTasks = LoadMyTasks(boards);

		return View(new DashboardViewModel {
			Boards = boards,
			TaskBuckets = TaskBucketing.Summarize(myTasks, DateTime.UtcNow.Date),
		});
	}

	public async Task<IActionResult> MyTasks() {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var boards = await _boards.GetAccessibleBoards(userId);
		return View(LoadMyTasks(boards));
	}

	private List<MyTaskItem> LoadMyTasks(IReadOnlyList<Board> boards) {
		var me = _currentUser.Username ?? "";

		return boards
			.SelectMany(b => b.Columns.SelectMany(c => c.Tasks
				.Where(t => t.AssignedUsers.Contains(me))
				.Select(t => new MyTaskItem {
					BoardId = b.Id.ToString(),
					BoardName = b.Name,
					TaskId = t.Id.ToString(),
					TaskName = t.Name,
					Column = c.Type,
					Priority = t.Priority,
					DueDate = t.DueDate,
					Done = t.CompletedAt is not null,
				})))
			.OrderBy(x => x.DueDate ?? DateTime.MaxValue)
			.ThenByDescending(x => x.Priority)
			.ToList();
	}
}
