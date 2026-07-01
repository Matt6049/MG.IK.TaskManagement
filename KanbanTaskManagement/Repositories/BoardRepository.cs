using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading.Tasks;

namespace KanbanTaskManagement.Repositories;

public class BoardRepository : DocumentRepository<Board> {
	public async Task<UpdateQueryResult<Board>> UpdateTask(string boardId, KanbanTask task) {
		if (!ObjectId.TryParse(boardId, out ObjectId _boardId))
			return new BadRequestResult();

		var filter = Builders<Board>.Filter.Eq(b => b.Id, _boardId);
		var update = Builders<Board>.Update.Set("Columns.$[col].Tasks.$[task]", task);
		var arrayFilters = new List<ArrayFilterDefinition> {
			new BsonDocumentArrayFilterDefinition<BsonDocument>(
				new BsonDocument("col.Tasks._id", task.Id)),
			new BsonDocumentArrayFilterDefinition<BsonDocument>(
				new BsonDocument("task._id", task.Id))
		};

		return await Collection.UpdateOneAsync(
			filter,
			update,
			new UpdateOptions { ArrayFilters = arrayFilters });
	}


	public async Task<UpdateQueryResult<Board>> CreateTask(string boardId, ColumnType colType, string taskId) {
		if (!ObjectId.TryParse(boardId, out ObjectId _boardId)
		|| !ObjectId.TryParse(taskId, out ObjectId _taskId))
			return new BadRequestResult();

		var filter = Builders<Board>.Filter.Eq(b => b.Id, _boardId);
		var update = Builders<Board>.Update.Push("Columns.$[col].Tasks", _taskId);
		var arrayFilters = new List<ArrayFilterDefinition>(){
			new BsonDocumentArrayFilterDefinition<BsonDocument>(
				new BsonDocument("col.Type", colType))
		};

		return await Collection.UpdateOneAsync(
			filter, 
			update, 
			new UpdateOptions { ArrayFilters = arrayFilters });
	}


	public async Task<UpdateQueryResult<Board>> DeleteTask(string boardId, string taskId) {
		if (!ObjectId.TryParse(boardId, out ObjectId _boardId)
		|| !ObjectId.TryParse(taskId, out ObjectId _taskId))
			return new BadRequestResult();

		var filter = Builders<Board>.Filter.Eq(b => b.Id, _boardId);
		var update = Builders<Board>.Update.PullFilter(
			"Columns.$[col].Tasks",
			Builders<KanbanTask>.Filter.Eq(t => t.Id, _taskId)
			);
		var arrayFilters = new List<ArrayFilterDefinition>(){
			new BsonDocumentArrayFilterDefinition<BsonDocument>(
				new BsonDocument("col.Tasks._id", _taskId))
		};

		return await Collection.UpdateOneAsync(
			filter,
			update,
			new UpdateOptions { ArrayFilters = arrayFilters });
	}


	public async Task<ReadQueryResult<KanbanTask>> GetTaskById(string boardId, string taskId) {
		if(!ObjectId.TryParse(taskId, out ObjectId _taskId))
			return new BadRequestResult();

		var boardRes = await GetById(boardId);
		if (!boardRes.Ok) 
			return boardRes.ErrorStatus;
		return await boardRes.Result.Columns
			.AsQueryable()
			.SelectMany(col => col.Tasks)
			.Where(task => task.Id == _taskId)
			.FirstOrDefaultAsync();
	}
}
