namespace CUChatNet.Api.Dtos;

public record SendMessageRequest(
    long SenderId,
    string Content,
    string Type = "text",
    string? MediaUrl = null,
    string? FileName = null,
    string? MimeType = null,
    long? SizeBytes = null,
    int? DurationSeconds = null
);

public record UpdateMessageStatusRequest(long UserId, string Status);

public record MessageDto(
    long Id,
    long ChatId,
    long SenderId,
    string SenderName,
    string Content,
    DateTime Timestamp,
    string Status,
    bool Encrypted,
    bool Edited,
    bool DeletedForMe,
    bool DeletedForAll,
    string Type,
    string? MediaUrl
);
