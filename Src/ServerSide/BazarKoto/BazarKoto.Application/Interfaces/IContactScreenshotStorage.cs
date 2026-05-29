using BazarKoto.Contracts.Contact;

namespace BazarKoto.Application.Interfaces;

public interface IContactScreenshotStorage
{
    Task<ContactScreenshotStorageResult> SaveAsync(ContactScreenshotUpload screenshot, CancellationToken cancellationToken = default);
}

public class ContactScreenshotStorageResult
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
