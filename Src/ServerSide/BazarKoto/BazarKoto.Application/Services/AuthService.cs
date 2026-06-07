using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Auth;
using BazarKoto.Contracts.Common;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedLoginAttempts = 5;
    private const int MinimumPasswordLength = 12;
    private const string CredentialUpdateFailureMessage = "Unable to update credentials.";
    private const string CredentialUpdateSuccessMessage = "Credentials updated. Please sign in again.";
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

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
        var now = DateTime.UtcNow;

        if (user is null || !user.IsActive || user.Role != UserRole.Admin)
        {
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        if (user.LockoutEndAt is not null && user.LockoutEndAt > now)
        {
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedLoginAttempts)
            {
                user.LockoutEndAt = now.Add(LockoutDuration);
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        var role = user.Role.ToString();
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, role, user.TokenVersion);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetAccessTokenExpiresAt(now);

        user.RefreshTokenHash = _passwordHasher.HashPassword(refreshToken);
        user.RefreshTokenExpiresAt = now.AddDays(7);
        user.LastLoginAt = now;
        user.FailedLoginCount = 0;
        user.LockoutEndAt = null;

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

    public async Task<ApiResponse<object>> UpdateAdminEmailAsync(Guid userId, UpdateAdminEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveAdminAsync(userId, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            return ApiResponse<object>.Fail(CredentialUpdateFailureMessage);
        }

        var newEmail = request.NewEmail?.Trim() ?? string.Empty;
        var emailValidationErrors = await ValidateNewEmailAsync(user, newEmail, cancellationToken);

        if (emailValidationErrors.Count > 0)
        {
            return ApiResponse<object>.Fail("Validation failed.", emailValidationErrors);
        }

        user.Email = newEmail;
        ApplyCredentialSecurityUpdate(user, updatePasswordChangedAt: false);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new object(), CredentialUpdateSuccessMessage);
    }

    public async Task<ApiResponse<object>> UpdateAdminPasswordAsync(Guid userId, UpdateAdminPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveAdminAsync(userId, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.OldPassword ?? string.Empty, user.PasswordHash))
        {
            return ApiResponse<object>.Fail(CredentialUpdateFailureMessage);
        }

        var passwordValidationErrors = ValidateNewPassword(request.NewPassword, request.ConfirmPassword, user.Email);

        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            passwordValidationErrors.Add("New password must be different from the old password.");
        }

        if (passwordValidationErrors.Count > 0)
        {
            return ApiResponse<object>.Fail("Validation failed.", passwordValidationErrors);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        ApplyCredentialSecurityUpdate(user, updatePasswordChangedAt: true);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new object(), CredentialUpdateSuccessMessage);
    }

    public async Task<ApiResponse<object>> UpdateAdminCredentialsAsync(Guid userId, UpdateAdminCredentialsRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveAdminAsync(userId, cancellationToken);

        if (user is null ||
            !string.Equals(request.OldEmail?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase) ||
            !_passwordHasher.VerifyPassword(request.OldPassword ?? string.Empty, user.PasswordHash))
        {
            return ApiResponse<object>.Fail(CredentialUpdateFailureMessage);
        }

        var newEmail = request.NewEmail?.Trim() ?? string.Empty;
        var validationErrors = await ValidateNewEmailAsync(user, newEmail, cancellationToken);
        validationErrors.AddRange(ValidateNewPassword(request.NewPassword, request.ConfirmPassword, user.Email, newEmail));

        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            validationErrors.Add("New password must be different from the old password.");
        }

        if (validationErrors.Count > 0)
        {
            return ApiResponse<object>.Fail("Validation failed.", validationErrors);
        }

        user.Email = newEmail;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        ApplyCredentialSecurityUpdate(user, updatePasswordChangedAt: true);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new object(), CredentialUpdateSuccessMessage);
    }

    private async Task<User?> GetActiveAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is not null && user.IsActive && user.Role == UserRole.Admin ? user : null;
    }

    private async Task<List<string>> ValidateNewEmailAsync(User user, string newEmail, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(newEmail))
        {
            errors.Add("New email is required.");
            return errors;
        }

        if (!IsValidEmail(newEmail))
        {
            errors.Add("New email must be a valid email address.");
            return errors;
        }

        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("New email must be different from the current email.");
            return errors;
        }

        var existingUser = await _userRepository.GetByNormalizedEmailAsync(NormalizeEmail(newEmail), cancellationToken);

        if (existingUser is not null && existingUser.Id != user.Id)
        {
            errors.Add("Email address is already in use.");
        }

        return errors;
    }

    private static List<string> ValidateNewPassword(string newPassword, string confirmPassword, params string[] emails)
    {
        var errors = new List<string>();
        newPassword ??= string.Empty;
        confirmPassword ??= string.Empty;

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            errors.Add("Confirm password must match new password.");
        }

        if (newPassword.Length < MinimumPasswordLength)
        {
            errors.Add("New password must be at least 12 characters long.");
        }

        if (!newPassword.Any(char.IsUpper))
        {
            errors.Add("New password must include at least one uppercase letter.");
        }

        if (!newPassword.Any(char.IsLower))
        {
            errors.Add("New password must include at least one lowercase letter.");
        }

        if (!newPassword.Any(char.IsDigit))
        {
            errors.Add("New password must include at least one digit.");
        }

        if (!newPassword.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("New password must include at least one special character.");
        }

        if (ContainsEmailLocalPart(newPassword, emails))
        {
            errors.Add("New password must not contain the admin email name.");
        }

        return errors;
    }

    private static bool ContainsEmailLocalPart(string password, IEnumerable<string> emails)
    {
        foreach (var email in emails)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var atIndex = email.IndexOf('@');
            var localPart = atIndex > 0 ? email[..atIndex] : string.Empty;

            if (localPart.Length >= 3 && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ApplyCredentialSecurityUpdate(User user, bool updatePasswordChangedAt)
    {
        user.TokenVersion++;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;

        if (updatePasswordChangedAt)
        {
            user.LastPasswordChangedAt = DateTime.UtcNow;
        }

        // TODO: Log AdminEmailUpdated/AdminPasswordUpdated/AdminCredentialsUpdated when IAdminAuditService has an implementation.
    }
}
