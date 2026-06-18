using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Data;

public record QueryResult<T> {
	public T? Result { get; set; }
	public ActionResult? ErrorStatus { get; set; }
	public bool Ok { get; set; } = false;
}
