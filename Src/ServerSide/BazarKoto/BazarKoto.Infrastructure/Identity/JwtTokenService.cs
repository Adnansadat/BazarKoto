using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using BazarKoto.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BazarKoto.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    public const string TokenVersionClaim = "tokenVersion";

    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email, string role, int tokenVersion)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var issuedAt = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            Expires = GetAccessTokenExpiresAt(issuedAt),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                [ClaimTypes.NameIdentifier] = userId.ToString(),
                [ClaimTypes.Email] = email,
                [ClaimTypes.Role] = role,
                [TokenVersionClaim] = tokenVersion
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public DateTime GetAccessTokenExpiresAt(DateTime utcNow)
    {
        return utcNow.AddMinutes(GetAccessTokenMinutes());
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private int GetAccessTokenMinutes()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : 60;
    }
}
