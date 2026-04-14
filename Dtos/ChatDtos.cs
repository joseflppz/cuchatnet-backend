namespace CUChatNet.Api.Dtos;

public record CreateDirectChatRequest(long CurrentUserId, long OtherUserId);

public class CreateGroupChatRequest
{
    public long CurrentUserId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupPhoto { get; set; }
    public string? GroupDescription { get; set; }
    public List<long> MemberIds { get; set; } = new List<long>();
}



public record ChatListItemDto
{
    public long Id { get; init; }
    public long ParticipantId { get; init; }
    public string ParticipantName { get; init; } = string.Empty;
    public string? ParticipantPhoto { get; init; }
    public string? ParticipantDescription { get; init; }
    public string? ParticipantStatus { get; init; }
    public string LastMessage { get; init; } = string.Empty;
    public DateTime? LastMessageTime { get; init; }
    public int Unread { get; init; }
    public bool Pinned { get; init; }
    public bool Archived { get; init; }
    public bool IsGroup { get; init; }
    public bool Silenced { get; init; }
}
public record GroupMemberDto(long Id, string Name, string Role);

public record GroupDetailDto(
    long Id,
    string Name,
    string? Photo,
    string? Description,
    string? Rules,
    DateTime CreatedAt,
    long CreatorId,
    string SendMessagesPermission,
    string EditInfoPermission,
    List<GroupMemberDto> Members
);

// Clase para la actualización completa desde el Modal de React
public class FullUpdateGroupRequest
{
    public long CurrentUserId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupDescription { get; set; }
    public string? GroupPhoto { get; set; }
    public string? GroupRules { get; set; }
    public bool OnlyAdminsCanPost { get; set; }
    public bool OnlyAdminsCanEdit { get; set; }
    public List<long> MemberIds { get; set; } = new List<long>();
    public List<long> AdminIds { get; set; } = new List<long>();
}