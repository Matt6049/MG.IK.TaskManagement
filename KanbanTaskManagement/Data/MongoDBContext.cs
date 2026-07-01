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
		this.UserCollection = db.GetCollection<User>(options.Value.UserCollectionName);
		this.GroupCollection = db.GetCollection<Group>(options.Value.GroupCollectionName);

		//var salt = GenerateSalt();
		//var bytePass = Encoding.UTF32.GetBytes(salt + "asdf2026");
		//User user = new() {
		//	Username = "jk2026",
		//	PasswordSalt = salt,
		//	PasswordHash = new string(Encoding.UTF32.GetChars(SHA256.HashData(bytePass))),
		//};
		//UserCollection.InsertOne(user);
		//var user = UserCollection.Find(_ => true).First();
		//var group = GroupCollection.Find(_ => true).First();
		//group.Members.Add(new GroupMember() { UserId = user.Id, Role = GroupRole.OWNER });
		//GroupCollection.UpdateOne(gr => gr.Id == group.Id, Builders<Group>.Update.Set(gr => gr.Members, group.Members));
	}

	private string GenerateSalt() {
		string salt = "";
		Random rand = new Random();
		for(int i=0; i<8; i++) {
			salt += Convert.ToChar(rand.Next(0, Int16.MaxValue));
		}
		return salt;
	}

	public IMongoCollection<Board> BoardCollection { get; private set; }
	public IMongoCollection<User> UserCollection { get; private set; }
	public IMongoCollection<Group> GroupCollection { get; private set; }
	

}
