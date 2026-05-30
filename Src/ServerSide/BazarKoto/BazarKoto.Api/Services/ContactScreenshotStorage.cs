using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Contact;

namespace BazarKoto.Api.Services;

public class ContactScreenshotStorage : IContactScreenshotStorage
{
    private const string UploadUrlPrefix = "uploads/contact-screenshots";
    private const string UploadDirectoryName = "contact-screenshots";
    private readonly IWebHostEnvironment _environment;

    public ContactScreenshotStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ContactScreenshotStorageResult> SaveAsync(ContactScreenshotUpload screenshot, CancellationToken cancellationToken = default)
    {
        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);

        var safeFileName = $"{Guid.NewGuid():N}{GetExtension(screenshot.ContentType)}";
        var destinationPath = Path.GetFullPath(Path.Combine(uploadRoot, safeFileName));

        if (!destinationPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid upload destination.");
        }

        if (screenshot.Content.CanSeek)
        {
            screenshot.Content.Position = 0;
        }

        await using (var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await screenshot.Content.CopyToAsync(destination, cancellationToken);
        }

        return new ContactScreenshotStorageResult
        {
            Url = $"{UploadUrlPrefix}/{safeFileName}",
            FileName = safeFileName,
            OriginalFileName = NormalizeOriginalFileName(screenshot.FileName),
            ContentType = screenshot.ContentType,
            SizeBytes = screenshot.Length
        };
    }

    public static string GetUploadRoot(IWebHostEnvironment environment)
    {
        var frontendPublicRoot = ResolveFrontendPublicRoot(environment);
        var uploadRoot = Path.GetFullPath(Path.Combine(frontendPublicRoot, "uploads", UploadDirectoryName));
        var expectedRoot = Path.GetFullPath(Path.Combine(frontendPublicRoot, "uploads"));

        if (!uploadRoot.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid upload root.");
        }

        return uploadRoot;
    }

    private static string ResolveFrontendPublicRoot(IWebHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            return Path.GetFullPath(environment.WebRootPath);
        }

        return Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "..",
            "..",
            "..",
            "ClientSide",
            "bazarKoto",
            "public"));
    }

    private string GetUploadRoot()
    {
        return GetUploadRoot(_environment);
    }

    private static string GetExtension(string contentType)
    {
        return contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            ? ".png"
            : contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
                ? ".jpg"
                : ".webp";
    }

    private static string NormalizeOriginalFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeName)
            ? "screenshot"
            : safeName[..Math.Min(safeName.Length, 255)];
    }
}
