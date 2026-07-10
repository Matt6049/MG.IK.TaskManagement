using KanbanTaskManagement.Models;
using KanbanTaskManagement.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace KanbanTaskManagement.Data; 

public class MongoDBContext{
	public MongoDBContext(IOptions<DbSettings> options) {
		var db = new MongoClient(options.Value.ConnectionString)
			.GetDatabase(options.Value.DatabaseName);
		this.BoardCollection = db.GetCollection<Board>(options.Value.BoardCollectionName);
		this.UserCollection = db.GetCollection<KanbanUser>(options.Value.UserCollectionName);
		this.GroupCollection = db.GetCollection<Group>(options.Value.GroupCollectionName);
	}

	public IMongoCollection<Board> BoardCollection { get; private set; }
	public IMongoCollection<KanbanUser> UserCollection { get; private set; }
	public IMongoCollection<Group> GroupCollection { get; private set; }
	

}
