using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class ContactService : IContactService
{
    private const long MaxScreenshotSizeBytes = 3 * 1024 * 1024;
    private const int MaxAdminNoteLength = 1000;
    private static readonly HashSet<string> AllowedScreenshotContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    private readonly IContactRepository _contactRepository;
    private readonly IContactScreenshotStorage _screenshotStorage;
    private readonly IUserTrackingRequestContextAccessor _requestContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public ContactService(
        IContactRepository contactRepository,
        IContactScreenshotStorage screenshotStorage,
        IUserTrackingRequestContextAccessor requestContextAccessor,
        IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _screenshotStorage = screenshotStorage;
        _requestContextAccessor = requestContextAccessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ContactMessageResponse>> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = await ValidateRequestAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<ContactMessageResponse>.Fail("Validation failed.", validationErrors);
        }

        ContactScreenshotStorageResult? screenshot = null;

        if (request.Screenshot is not null)
        {
            screenshot = await _screenshotStorage.SaveAsync(request.Screenshot, cancellationToken);
        }

        var contactMessage = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            ScreenshotUrl = screenshot?.Url,
            ScreenshotFileName = screenshot?.FileName,
            ScreenshotOriginalFileName = screenshot?.OriginalFileName,
            ScreenshotContentType = screenshot?.ContentType,
            ScreenshotSizeBytes = screenshot?.SizeBytes,
            Status = ContactMessageStatus.New,
            IpAddress = TrimToMaxLength(_requestContextAccessor.RawIpAddress, 64),
            UserAgent = TrimToMaxLength(_requestContextAccessor.RawUserAgent, 512)
        };

        await _contactRepository.AddAsync(contactMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ContactMessageResponse>.Ok(ToResponse(contactMessage), "Your message has been submitted successfully.");
    }

    public async Task<PagedResponse<ContactMessageListItemResponse>> GetContactMessagesAsync(ContactMessageSearchRequest request, CancellationToken cancellationToken = default)
    {
        var status = NormalizeStatusFilter(request.Status);
        var messages = await _contactRepository.GetAsync(request.Search, status, request.DateFrom, request.DateTo, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _contactRepository.CountAsync(request.Search, status, request.DateFrom, request.DateTo, cancellationToken);

        return new PagedResponse<ContactMessageListItemResponse>
        {
            Success = true,
            Message = "Success",
            Data = messages.Select(ToListItemResponse).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<ApiResponse<ContactMessageResponse>> GetContactMessageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contactMessage = await _contactRepository.GetByIdAsync(id, cancellationToken);

        if (contactMessage is null)
        {
            return ApiResponse<ContactMessageResponse>.Fail("Contact message was not found.");
        }

        if (contactMessage.Status == ContactMessageStatus.New)
        {
            contactMessage.Status = ContactMessageStatus.Read;
            contactMessage.ReadAt = DateTime.UtcNow;
            _contactRepository.Update(contactMessage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<ContactMessageResponse>.Ok(ToResponse(contactMessage));
    }

    public async Task<ApiResponse<ContactMessageResponse>> UpdateContactMessageStatusAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseAllowedStatus(request.Status, out var status))
        {
            return ApiResponse<ContactMessageResponse>.Fail("Validation failed.", ["Status is not valid."]);
        }

        var contactMessage = await _contactRepository.GetByIdAsync(id, cancellationToken);

        if (contactMessage is null)
        {
            return ApiResponse<ContactMessageResponse>.Fail("Contact message was not found.");
        }

        contactMessage.Status = status;

        if (status == ContactMessageStatus.Read && !contactMessage.ReadAt.HasValue)
        {
            contactMessage.ReadAt = DateTime.UtcNow;
        }

        if (status == ContactMessageStatus.InProgress && !contactMessage.ReadAt.HasValue)
        {
            contactMessage.ReadAt = DateTime.UtcNow;
        }

        if (status == ContactMessageStatus.Resolved)
        {
            contactMessage.ResolvedAt ??= DateTime.UtcNow;
        }
        else if (contactMessage.ResolvedAt.HasValue)
        {
            contactMessage.ResolvedAt = null;
        }

        _contactRepository.Update(contactMessage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ContactMessageResponse>.Ok(ToResponse(contactMessage), "Contact message status updated successfully.");
    }

    public async Task<ApiResponse<ContactMessageResponse>> UpdateContactMessageNoteAsync(Guid id, UpdateContactMessageNoteRequest request, CancellationToken cancellationToken = default)
    {
        var adminNote = request.AdminNote?.Trim();

        if (adminNote?.Length > MaxAdminNoteLength)
        {
            return ApiResponse<ContactMessageResponse>.Fail("Validation failed.", [$"Admin note must be {MaxAdminNoteLength} characters or fewer."]);
        }

        var contactMessage = await _contactRepository.GetByIdAsync(id, cancellationToken);

        if (contactMessage is null)
        {
            return ApiResponse<ContactMessageResponse>.Fail("Contact message was not found.");
        }

        contactMessage.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote;
        _contactRepository.Update(contactMessage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ContactMessageResponse>.Ok(ToResponse(contactMessage), "Admin note updated successfully.");
    }

    private static ContactMessageListItemResponse ToListItemResponse(ContactMessage contactMessage)
    {
        return new ContactMessageListItemResponse
        {
            Id = contactMessage.Id,
            Name = contactMessage.Name,
            Email = contactMessage.Email,
            Subject = contactMessage.Subject,
            MessagePreview = CreatePreview(contactMessage.Message),
            Status = contactMessage.Status.ToString(),
            HasScreenshot = !string.IsNullOrWhiteSpace(contactMessage.ScreenshotUrl),
            CreatedAt = contactMessage.CreatedAt,
            ReadAt = contactMessage.ReadAt,
            ResolvedAt = contactMessage.ResolvedAt
        };
    }

    private static string CreatePreview(string value)
    {
        const int previewLength = 120;
        var normalized = value.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= previewLength
            ? normalized
            : $"{normalized[..previewLength]}...";
    }

    private static ContactMessageResponse ToResponse(ContactMessage contactMessage)
    {
        return new ContactMessageResponse
        {
            Id = contactMessage.Id,
            Name = contactMessage.Name,
            Email = contactMessage.Email,
            Subject = contactMessage.Subject,
            Message = contactMessage.Message,
            ScreenshotUrl = contactMessage.ScreenshotUrl,
            ScreenshotFileName = contactMessage.ScreenshotFileName,
            ScreenshotOriginalFileName = contactMessage.ScreenshotOriginalFileName,
            ScreenshotContentType = contactMessage.ScreenshotContentType,
            ScreenshotSizeBytes = contactMessage.ScreenshotSizeBytes,
            Status = contactMessage.Status.ToString(),
            AdminNote = contactMessage.AdminNote,
            IpAddress = contactMessage.IpAddress,
            UserAgent = contactMessage.UserAgent,
            BrowserName = contactMessage.BrowserName,
            DeviceType = contactMessage.DeviceType,
            OS = contactMessage.OS,
            ReadAt = contactMessage.ReadAt,
            ResolvedAt = contactMessage.ResolvedAt,
            CreatedAt = contactMessage.CreatedAt,
            UpdatedAt = contactMessage.UpdatedAt
        };
    }

    private static async Task<List<string>> ValidateRequestAsync(CreateContactMessageRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        ValidateLength(request.Name, "Name", 2, 80, errors);
        ValidateEmail(request.Email, errors);
        ValidateLength(request.Subject, "Subject", 5, 150, errors);
        ValidateLength(request.Message, "Message", 20, 2000, errors);

        if (request.Screenshot is not null)
        {
            await ValidateScreenshotAsync(request.Screenshot, errors, cancellationToken);
        }

        return errors;
    }

    private static void ValidateLength(string value, string fieldName, int minLength, int maxLength, List<string> errors)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add($"{fieldName} is required.");
            return;
        }

        if (normalizedValue.Length < minLength)
        {
            errors.Add($"{fieldName} must be at least {minLength} characters.");
        }

        if (normalizedValue.Length > maxLength)
        {
            errors.Add($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private static void ValidateEmail(string value, List<string> errors)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add("Email is required.");
            return;
        }

        if (normalizedValue.Length > 120)
        {
            errors.Add("Email must be 120 characters or fewer.");
        }

        if (!System.Net.Mail.MailAddress.TryCreate(normalizedValue, out var emailAddress)
            || !string.Equals(emailAddress.Address, normalizedValue, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Email must be a valid email address.");
        }
    }

    private static async Task ValidateScreenshotAsync(ContactScreenshotUpload screenshot, List<string> errors, CancellationToken cancellationToken)
    {
        if (screenshot.Length <= 0)
        {
            errors.Add("Screenshot file cannot be empty.");
            return;
        }

        if (screenshot.Length > MaxScreenshotSizeBytes)
        {
            errors.Add("Screenshot file size must be 3 MB or smaller.");
        }

        if (!AllowedScreenshotContentTypes.Contains(screenshot.ContentType))
        {
            errors.Add("Screenshot must be a PNG, JPEG, or WebP image.");
            return;
        }

        if (!await HasValidImageSignatureAsync(screenshot, cancellationToken))
        {
            errors.Add("Screenshot file content is not a supported image.");
        }
    }

    private static async Task<bool> HasValidImageSignatureAsync(ContactScreenshotUpload screenshot, CancellationToken cancellationToken)
    {
        if (!screenshot.Content.CanSeek)
        {
            return false;
        }

        var originalPosition = screenshot.Content.Position;
        screenshot.Content.Position = 0;

        var buffer = new byte[12];
        var bytesRead = await screenshot.Content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        screenshot.Content.Position = originalPosition;

        return screenshot.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            ? bytesRead >= 8
                && buffer[0] == 0x89
                && buffer[1] == 0x50
                && buffer[2] == 0x4E
                && buffer[3] == 0x47
                && buffer[4] == 0x0D
                && buffer[5] == 0x0A
                && buffer[6] == 0x1A
                && buffer[7] == 0x0A
            : screenshot.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
                ? bytesRead >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF
                : bytesRead >= 12
                    && buffer[0] == 0x52
                    && buffer[1] == 0x49
                    && buffer[2] == 0x46
                    && buffer[3] == 0x46
                    && buffer[8] == 0x57
                    && buffer[9] == 0x45
                    && buffer[10] == 0x42
                    && buffer[11] == 0x50;
    }

    private static string? TrimToMaxLength(string? value, int maxLength)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue[..Math.Min(normalizedValue.Length, maxLength)];
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        return TryParseAllowedStatus(status, out var parsed) ? parsed.ToString() : null;
    }

    private static bool TryParseAllowedStatus(string? status, out ContactMessageStatus parsed)
    {
        if (Enum.TryParse(status, true, out parsed))
        {
            return parsed is ContactMessageStatus.New
                or ContactMessageStatus.Read
                or ContactMessageStatus.InProgress
                or ContactMessageStatus.Resolved
                or ContactMessageStatus.Spam;
        }

        parsed = ContactMessageStatus.New;
        return false;
    }
}
