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
    private readonly BoardRepository repository;

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
        if(!queryRes.Ok)
			return queryRes.ErrorStatus;
		return View(queryRes.Result);
	}


    public async Task<IActionResult> UpdateTask(string boardId, string taskId, string newName, string newDescription) {
        var taskRes = await repository.GetTaskById(boardId, taskId);
        if (!taskRes.Ok)
            return taskRes.ErrorStatus;
        var task = taskRes.Result;
        task.Name = newName;
        task.Description = newDescription;

        var updateRes = await repository.UpdateTask(boardId, task);
        if (!updateRes.Ok)
            return updateRes.ErrorStatus;
        return View(task);
    }
}