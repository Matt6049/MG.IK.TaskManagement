using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models;


public class Group : IMongoDocument{
	public ObjectId Id { get; set; }

	public string? Name { get; set; } = null;

	[BsonRequired]
	[BsonDateTimeOptions(
		Kind = DateTimeKind.Utc,
		Representation = BsonType.DateTime
	)]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	[BsonRequired]
	public List<GroupMember> Members { get; set; } = [];

	public GroupRole? RoleOf(ObjectId userId)
		=> Members.Where(m => m.UserId == userId).Select(m => (GroupRole?) m.Role).FirstOrDefault();

	public bool IsManagedBy(ObjectId userId)
		=> RoleOf(userId) >= GroupRole.ADMIN;
}
