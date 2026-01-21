using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AgenticCommerce.API.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task<User?> GetUserByIdAsync(Guid userId);
}

public class AuthService : IAuthService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AgenticCommerceDbContext db,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return AuthResult.Failure("Email already registered");
        }

        // Create organization
        var org = new Organization
        {
            Name = request.OrganizationName,
            Slug = GenerateSlug(request.OrganizationName)
        };

        // Ensure slug is unique
        var baseSlug = org.Slug;
        var counter = 1;
        while (await _db.Organizations.AnyAsync(o => o.Slug == org.Slug))
        {
            org.Slug = $"{baseSlug}-{counter++}";
        }

        _db.Organizations.Add(org);

        // Create user as owner
        var user = new User
        {
            OrganizationId = org.Id,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Name = request.Name,
            Role = UserRoles.Owner
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New organization registered: {OrgName} ({OrgId})", org.Name, org.Id);

        // Generate tokens
        var accessToken = GenerateAccessToken(user, org);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return AuthResult.Success(accessToken, refreshToken.Token, user, org);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult.Failure("Invalid email or password");
        }

        var accessToken = GenerateAccessToken(user, user.Organization);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        _logger.LogInformation("User logged in: {Email} (Org: {OrgId})", user.Email, user.OrganizationId);

        return AuthResult.Success(accessToken, refreshToken.Token, user, user.Organization);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Organization)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null)
        {
            return AuthResult.Failure("Invalid refresh token");
        }

        if (!token.IsActive)
        {
            return AuthResult.Failure("Refresh token expired or revoked");
        }

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;

        // Create new tokens
        var user = token.User;
        var accessToken = GenerateAccessToken(user, user.Organization);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        await _db.SaveChangesAsync();

        return AuthResult.Success(accessToken, newRefreshToken.Token, user, user.Organization);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    private string GenerateAccessToken(User user, Organization org)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name ?? user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("organization_id", org.Id.ToString()),
            new Claim("organization_name", org.Name)
        };

        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "15");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "AgentRails",
            audience: _config["Jwt:Audience"] ?? "AgentRails",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateRandomToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return refreshToken;
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-");
    }
}

// Request/Response DTOs
public record RegisterRequest(
    string Email,
    string Password,
    string Name,
    string OrganizationName
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshRequest(
    string RefreshToken
);

public class AuthResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public UserInfo? User { get; private set; }

    public static AuthResult Success(string accessToken, string refreshToken, User user, Organization org)
    {
        return new AuthResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                OrganizationId = org.Id,
                OrganizationName = org.Name
            }
        };
    }

    public static AuthResult Failure(string error)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Error = error
        };
    }
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}
