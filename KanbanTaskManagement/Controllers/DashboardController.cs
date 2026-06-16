using KanbanTaskManagement.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

public class DashboardController : Controller
{
    private readonly MongoDBContext Context;
    public DashboardController(MongoDBContext context) => Context = context;

    public async Task<IActionResult> Index()
    {
        // Pobiera wszystkie tablice do listy
        var boards = await Context.BoardCollection.Find(_ => true).ToListAsync();
        return View(boards);
    }
}