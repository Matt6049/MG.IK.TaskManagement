using KanbanTaskManagement.Data;
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
		var me = _currentUser.Username ?? "";
		var today = DateTime.UtcNow.Date;

		var myDue = boards
			.SelectMany(b => b.Columns.SelectMany(c => c.Tasks))
			.Where(t => t.CompletedAt is null && t.DueDate is not null && t.AssignedUsers.Contains(me))
			.Select(t => t.DueDate!.Value.Date)
			.ToList();

		return View(new DashboardViewModel {
			Boards = boards,
			OverdueCount = myDue.Count(d => d < today),
			TodayCount = myDue.Count(d => d == today),
		});
	}

	public async Task<IActionResult> MyTasks() {
		if (_currentUser.UserId is not { } userId)
			return Forbid();

		var boards = await _boards.GetAccessibleBoards(userId);
		var me = _currentUser.Username ?? "";

		var items = boards
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

		return View(items);
	}
}
