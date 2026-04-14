namespace CUChatNet.Api.Dtos;

public record SendVerificationRequest(string Email);

public record CheckVerificationRequest(string Email, string Code);

public record SetupProfileRequest(
    string Email,
    string Name,
    string? Phone,
    string? Description,
    string? PhotoUrl,
    string Status = "available"
);

public record AdminLoginRequest(string Email, string Password);

public record VerifyResponse(
    bool Success,
    string Message,
    bool UserExists = false,
    object? User = null,
    string? DevelopmentCode = null
);

public record AuthUserDto(
    long Id,
    string Phone,
    string? Email,
    string Name,
    string? Photo,
    string? Description,
    string Status,
    string Role,
    DateTime CreatedAt,
    bool Active
);