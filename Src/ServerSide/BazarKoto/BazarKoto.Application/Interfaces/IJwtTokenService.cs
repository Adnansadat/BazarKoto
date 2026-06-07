namespace BazarKoto.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, int tokenVersion);
    DateTime GetAccessTokenExpiresAt(DateTime utcNow);
    string GenerateRefreshToken();
}
