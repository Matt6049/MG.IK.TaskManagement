using KanbanTaskManagement.Data;
using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace KanbanTaskManagement.Repositories;

public class DocumentRepository<TDocument> : Repository<TDocument> where TDocument: IMongoDocument{
	public required IMongoCollection<TDocument> Collection { get; init; }

	public async Task<ReadQueryResult<TDocument>> GetById(string id) {
		if (!ObjectId.TryParse(id, out ObjectId documentId)) {
			return new BadRequestResult();
		}

		return await Collection.AsQueryable()
			.Where(doc => doc.Id == documentId)
			.FirstOrDefaultAsync();
	}
}
