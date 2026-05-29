namespace BazarKoto.Contracts.Contact;

public class ContactMessageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ScreenshotUrl { get; set; }
    public string? ScreenshotFileName { get; set; }
    public string? ScreenshotOriginalFileName { get; set; }
    public string? ScreenshotContentType { get; set; }
    public long? ScreenshotSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? BrowserName { get; set; }
    public string? DeviceType { get; set; }
    public string? OS { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
