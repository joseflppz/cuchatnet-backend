namespace CUChatNet.Api.Dtos;

public record CreateStateRequest(long UserId, string Content, string Type = "text", string? MediaUrl = null);
public record ViewStateRequest(long UserId);

public record StateDto(
    long Id,
    long UserId,
    string UserName,
    string? UserPhoto,
    string Content,
    string Type,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    List<long> ViewedBy
);
