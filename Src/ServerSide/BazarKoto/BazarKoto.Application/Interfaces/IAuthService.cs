using BazarKoto.Contracts.Auth;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
