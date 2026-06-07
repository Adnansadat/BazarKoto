using BazarKoto.Contracts.Auth;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> UpdateAdminEmailAsync(Guid userId, UpdateAdminEmailRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> UpdateAdminPasswordAsync(Guid userId, UpdateAdminPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> UpdateAdminCredentialsAsync(Guid userId, UpdateAdminCredentialsRequest request, CancellationToken cancellationToken = default);
}
