using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanbanTaskManagement.Models
{
    public enum TaskPriority
    {
        LOW,
        MEDIUM,
        HIGH
    }


    public class KanbanTask
    {
        [BsonId]
        [BsonRequired]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        [BsonRequired]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [BsonRequired]
        [BsonRepresentation(BsonType.Int32)]
        public TaskPriority Priority { get; set; } = TaskPriority.LOW;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? StartDate { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? DueDate { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        [BsonRequired]
        public required string CreatorName { get; set; }

        [BsonRequired]
        public string[] AssignedUsers { get; set; } = Array.Empty<string>();
    }
}