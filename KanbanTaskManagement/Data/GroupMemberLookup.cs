using KanbanTaskManagement.Models;

namespace KanbanTaskManagement.Data;

public class GroupMemberLookup : GroupMember{
	public required List<User> Matches { get; set; }
}
