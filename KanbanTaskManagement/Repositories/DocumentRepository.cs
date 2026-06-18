using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Repositories;

public class DocumentRepository<T> : Repository<T> where T: IMongoDocument{
	public required IMongoCollection<T> Collection { get; init; }

	public async Task<QueryResult<T>> GetById(string id) {
		QueryResult<T> queryRes = new();

		if (!ObjectId.TryParse(id, out ObjectId documentId)) {
			queryRes.ErrorStatus = new BadRequestResult();
			return queryRes;
		}

		var document = await Collection
			.Find(doc => doc.Id == documentId)
			.FirstOrDefaultAsync();
		if (document == null) {
			queryRes.ErrorStatus = new NotFoundResult();
			return queryRes;
		}
		queryRes.Ok = true;
		queryRes.Result = document;
		return queryRes;
	}
}
