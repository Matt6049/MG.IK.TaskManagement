using KanbanTaskManagement.Data;
using KanbanTaskManagement.Repositories;
using KanbanTaskManagement.Services;
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
		return View(boards);
	}
}
