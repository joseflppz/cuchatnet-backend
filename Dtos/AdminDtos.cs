namespace CUChatNet.Api.Dtos;

public record AdminUserDto(
    long Id,
    string Phone,
    string Name,
    string Status,
    DateTime CreatedAt,
    DateTime? LastActive,
    string? Email
);

public record CreateAdminUserRequest(
    string CountryCode,
    string PhoneNumber,
    string Name,
    string? Email,
    string Status = "active"
);

public record UpdateAdminUserRequest(
    string Name,
    string? Email,
    string Status
);

public record AdminLogDto(
    long Id,
    DateTime Timestamp,
    string Action,
    string User,
    string? Details,
    string Severity
);

public record AdminConfigDto(
    string AppName,
    int MaxGroupSize,
    int MessageTimeout,
    int MaxFileSize,
    string? SmtpServer,
    int? SmtpPort,
    string? SmsProvider,
    string? ApiEndpoint,
    bool MaintenanceMode,
    bool E2ERequired,
    int AutoArchiveInactivity
);

public record UpdateAdminConfigRequest(
    string AppName,
    int MaxGroupSize,
    int MessageTimeout,
    int MaxFileSize,
    string? SmtpServer,
    int? SmtpPort,
    string? SmsProvider,
    string? ApiEndpoint,
    bool MaintenanceMode,
    bool E2ERequired,
    int AutoArchiveInactivity
);

public record SecurityPolicyDto(
    int BlockAfterFailedAttempts,
    int BlockDuration,
    int KeyRotationDays,
    bool EnableDeviceDetection,
    bool EnableIdentityVerification,
    bool RequireE2EEncryption,
    int SuspiciousLoginTimeout,
    int MaxDevicesPerUser
);

public record UpdateSecurityPolicyRequest(
    int BlockAfterFailedAttempts,
    int BlockDuration,
    int KeyRotationDays,
    bool EnableDeviceDetection,
    bool EnableIdentityVerification,
    bool RequireE2EEncryption,
    int SuspiciousLoginTimeout,
    int MaxDevicesPerUser
);

public record AdminGroupDto(
    long Id,
    string Name,
    string Creator,
    int Members,
    DateTime Created,
    int Messages,
    string? Description
);

public record AdminMessageDto(
    long Id,
    string Sender,
    string Type,
    string Destination,
    string ConversationId,
    DateTime Timestamp,
    string Status,
    bool Encrypted,
    string? Device,
    string? Ip,
    DateTime? DeliveredAt,
    DateTime? SeenAt
);
