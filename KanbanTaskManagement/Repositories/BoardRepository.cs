using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading.Tasks;

namespace KanbanTaskManagement.Repositories;

public class BoardRepository : DocumentRepository<Board> {
	public async Task InsertBoard(Board board) {
		await Collection.InsertOneAsync(board);
	}

	public async Task<UpdateQueryResult<Board>> SetName(ObjectId boardId, string name) {
		return await Collection.UpdateOneAsync(
			b => b.Id == boardId,
			Builders<Board>.Update.Set(b => b.Name, name));
	}

	public async Task<DeleteResult> Delete(ObjectId boardId) {
		return await Collection.DeleteOneAsync(b => b.Id == boardId);
	}

	public async Task<ReadQueryResult<Board>> GetByName(string boardName) {
		var res = await Collection.FindAsync(board => board.Name == boardName);
		return await res.FirstOrDefaultAsync();
	}

	public async Task<UpdateQueryResult<Board>> AddMember(ObjectId boardId, BoardMember member) {
		return await Collection.UpdateOneAsync(
			b => b.Id == boardId,
			Builders<Board>.Update.Push(b => b.Members, member));
	}

	public async Task<UpdateQueryResult<Board>> SetMemberRole(ObjectId boardId, ObjectId userId, GroupRole role) {
		return await Collection.UpdateOneAsync(
			Builders<Board>.Filter.Eq(b => b.Id, boardId)
				& Builders<Board>.Filter.ElemMatch(b => b.Members, m => m.UserId == userId),
			Builders<Board>.Update.Set("Members.$.Role", (int) role));
	}

	public async Task<UpdateQueryResult<Board>> RemoveMember(ObjectId boardId, ObjectId userId) {
		return await Collection.UpdateOneAsync(
			b => b.Id == boardId,
			Builders<Board>.Update.PullFilter(b => b.Members, m => m.UserId == userId));
	}

	public async Task<UpdateQueryResult<Board>> SetGroup(ObjectId boardId, ObjectId groupId) {
		return await Collection.UpdateOneAsync(
			b => b.Id == boardId,
			Builders<Board>.Update.Set(b => b.GroupId, groupId));
	}

	public async Task<UpdateQueryResult<Board>> SetTheme(ObjectId boardId, BoardTheme theme) {
		return await Collection.UpdateOneAsync(
			b => b.Id == boardId,
			Builders<Board>.Update.Set(b => b.Theme, theme));
	}

	public async Task<List<Board>> GetAccessibleBoards(ObjectId userId) {
		var myGroupIds = await Database.GroupCollection
			.Find(Builders<Group>.Filter.ElemMatch(g => g.Members, m => m.UserId == userId))
			.Project(g => g.Id)
			.ToListAsync();

		var filter = Builders<Board>.Filter.Or(
			Builders<Board>.Filter.Eq(b => b.OwnerId, userId),
			Builders<Board>.Filter.ElemMatch(b => b.Members, m => m.UserId == userId),
			Builders<Board>.Filter.In(b => b.GroupId, myGroupIds));

		return await Collection.Find(filter).ToListAsync();
	}

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


	public async Task<UpdateQueryResult<Board>> InsertTask(Board board, ColumnType colType, KanbanTask task) {
		var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
		var update = Builders<Board>.Update.Push("Columns.$[col].Tasks", task);
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


	public async Task<UpdateQueryResult<Board>> MoveTask(string boardId, string taskId, ColumnType target, KanbanTask task) {
		if (!ObjectId.TryParse(boardId, out ObjectId _boardId)
		|| !ObjectId.TryParse(taskId, out ObjectId _taskId))
			return new BadRequestResult();

		var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, _boardId);

		var pull = await Collection.UpdateOneAsync(
			boardFilter,
			Builders<Board>.Update.PullFilter(
				"Columns.$[].Tasks",
				Builders<KanbanTask>.Filter.Eq(t => t.Id, _taskId)));

		if (!pull.IsAcknowledged || pull.ModifiedCount == 0)
			return pull;

		var push = await Collection.UpdateOneAsync(
			boardFilter,
			Builders<Board>.Update.Push("Columns.$[col].Tasks", task),
			new UpdateOptions {
				ArrayFilters = new List<ArrayFilterDefinition> {
					new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("col.Type", (int) target)),
				},
			});

		if (push.IsAcknowledged && push.ModifiedCount == 0)
			return new NotFoundResult();

		return push;
	}
}
