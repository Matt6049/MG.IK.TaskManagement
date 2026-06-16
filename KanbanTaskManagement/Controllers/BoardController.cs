using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Controllers;
public class BoardController : Controller {
	private readonly MongoDBContext Context;

	public BoardController(MongoDBContext context) {
		Context = context;
	}

	public async Task<IActionResult> Index(string id) {
		if(!ObjectId.TryParse(id, out ObjectId boardId)) {
			return BadRequest("Invalid Board ID format");
		}

		Board board = Context
			.BoardCollection
			.Find(Builders<Board>.Filter.Eq(board => board.Id, boardId)).FirstOrDefault();
		
		if(board == null)
			return BadRequest("Board not found");
		return View(board);
	}
}
