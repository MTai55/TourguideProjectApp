using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Auth;
using TourGuideAPI.Models;

namespace TourGuideAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
    Task<AuthResponseDto?> GenerateTokenAsync(int userId);  // DEBUG: generate token for any user
}

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email.ToLower(),
            Phone = dto.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role is "Owner" ? "Owner" : "User"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return await BuildResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await BuildResponse(user);
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || !token.IsActive) return null;

        // Revoke token cũ (rotation — mỗi lần refresh tạo token mới)
        token.IsRevoked = true;
        await db.SaveChangesAsync();

        return await BuildResponse(token.User!);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token == null) return;
        token.IsRevoked = true;
        await db.SaveChangesAsync();
    }

    // DEBUG: Generate JWT for any user by ID (no password required)
    public async Task<AuthResponseDto?> GenerateTokenAsync(int userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
        if (user == null) return null;
        return await BuildResponse(user);
    }

    // ── Private helpers ───────────────────────────────────────────
    private async Task<AuthResponseDto> BuildResponse(User user)
    {
        // JWT ngắn hạn 15 phút
        var expiry = DateTime.UtcNow.AddMinutes(
            config.GetValue<int>("Jwt:ExpiryMinutes", 15));

        // Tạo refresh token mới
        var rt = new RefreshToken
        {
            UserId = user.UserId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();

        return new AuthResponseDto(
            user.UserId, user.FullName, user.Email, user.Role,
            GenerateJwt(user, expiry), rt.Token, expiry);
    }

    private string GenerateJwt(User user, DateTime expiry)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim("fullName",                user.FullName)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}