using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using KanbanTaskManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using System.ComponentModel;


namespace KanbanTaskManagement.Controllers;

public class BoardController : Controller
{
    private readonly MongoDBContext _db;
    private readonly DocumentRepository<Board> repository;

    public BoardController(MongoDBContext db)
    {
        _db = db;
        repository = new() {
            Collection = _db.BoardCollection,
            Database = _db
        };
    }

    // Zmieniamy na publiczny GET (domyślnie)
    public async Task<IActionResult> Index(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // Pobieramy konkretną tablicę po jej ID
        var queryRes = await repository.GetById(id);
        if(queryRes.Ok)
			return View(queryRes.Result);
        return queryRes.ErrorStatus;
	}


    public async Task<IActionResult> UpdateTask(string boardId, string taskId, string newName, string newDescription) {
        var res = await GetBoardById(boardId);
        if (!res.Ok)
            return res.ErrorStatus;

    }

    private async Task<QueryResult<Board>> GetBoardById(string id) {


    }
}