using KanbanTaskManagement.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;


namespace KanbanTaskManagement.Controllers;

public class BoardController : Controller
{
    private readonly MongoDBContext Context;

    public BoardController(MongoDBContext context)
    {
        Context = context;
    }

    // Zmieniamy na publiczny GET (domyślnie)
    public async Task<IActionResult> Index(string id)
    {

        if (string.IsNullOrEmpty(id))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        // Pobieramy konkretną tablicę po jej ID
        var board = await Context.BoardCollection
            .Find(b => b.Id == ObjectId.Parse(id))
            .FirstOrDefaultAsync();

        if (board == null) return NotFound();

        return View(board);
    }
}