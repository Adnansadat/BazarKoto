using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Auth;
using BazarKoto.Contracts.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive || user.Role != UserRole.Admin)
        {
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        var role = user.Role.ToString();
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, role);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        user.RefreshTokenHash = _passwordHasher.HashPassword(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Email = user.Email,
            Role = role
        };

        return ApiResponse<LoginResponse>.Ok(response, "Login successful.");
    }

    public Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<LoginResponse>.Fail("Token refresh is not configured yet."));
    }

    public async Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is not null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<object>.Ok(new object(), "Logged out.");
    }
}
