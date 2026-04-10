namespace TourGuideAPI.DTOs.Auth;

public record RegisterDto(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string Role = "User"
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record UpdateProfileDto(
    string  FullName,
    string  Email,
    string? Phone
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);