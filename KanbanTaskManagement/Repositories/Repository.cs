using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Repositories;

public abstract class Repository<T>{
	public required MongoDBContext Database { get; init; }
}
