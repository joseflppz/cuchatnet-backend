namespace CUChatNet.Api.Dtos;

public record AddContactRequest(long UserId, long ContactUserId, bool FromAgenda = true);

public record ContactDto(
    long Id,
    string Name,
    string Phone,
    string? Photo,
    string? Description,
    string Status,
    bool FromAgenda
);
