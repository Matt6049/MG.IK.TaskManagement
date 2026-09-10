using MongoDB.Bson;

namespace KanbanTaskManagement.ViewModels;

public class GroupMembersViewModel {
	public ObjectId GroupId { get; set; }
	public bool CanManage { get; set; }
	public List<GroupMemberDTO> Members { get; set; } = [];
}
