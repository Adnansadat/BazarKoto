using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.Contact;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BazarKoto.Tests;

public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _contactRepository = new();
    private readonly Mock<IContactScreenshotStorage> _screenshotStorage = new();
    private readonly Mock<IUserTrackingRequestContextAccessor> _requestContextAccessor = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task CreateContactMessageAsync_WithoutScreenshot_SavesMessage()
    {
        ContactMessage? savedMessage = null;
        _contactRepository.Setup(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMessage, CancellationToken>((message, _) => savedMessage = message)
            .Returns(Task.CompletedTask);

        var response = await CreateService().CreateContactMessageAsync(CreateValidRequest());

        response.Success.Should().BeTrue();
        savedMessage.Should().NotBeNull();
        savedMessage!.Name.Should().Be("Test User");
        savedMessage.Email.Should().Be("test@example.com");
        savedMessage.ScreenshotUrl.Should().BeNull();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WithScreenshot_SavesMessageAndScreenshotUrl()
    {
        ContactMessage? savedMessage = null;
        _contactRepository.Setup(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMessage, CancellationToken>((message, _) => savedMessage = message)
            .Returns(Task.CompletedTask);
        _screenshotStorage.Setup(x => x.SaveAsync(It.IsAny<ContactScreenshotUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactScreenshotStorageResult
            {
                Url = "uploads/contact-screenshots/safe-file.png",
                FileName = "safe-file.png",
                OriginalFileName = "screen.png",
                ContentType = "image/png",
                SizeBytes = 12
            });

        var request = CreateValidRequest();
        request.Screenshot = CreatePngScreenshot();

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeTrue();
        savedMessage.Should().NotBeNull();
        savedMessage!.ScreenshotUrl.Should().Be("uploads/contact-screenshots/safe-file.png");
        savedMessage.ScreenshotFileName.Should().Be("safe-file.png");
        savedMessage.ScreenshotOriginalFileName.Should().Be("screen.png");
        savedMessage.ScreenshotContentType.Should().Be("image/png");
        savedMessage.ScreenshotSizeBytes.Should().Be(12);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WithInvalidEmail_IsRejected()
    {
        var request = CreateValidRequest();
        request.Email = "not-an-email";

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain(error => error.Contains("valid email", StringComparison.OrdinalIgnoreCase));
        _contactRepository.Verify(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WithTooShortMessage_IsRejected()
    {
        var request = CreateValidRequest();
        request.Message = "Too short";

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain(error => error.Contains("Message must be at least 20 characters", StringComparison.OrdinalIgnoreCase));
        _contactRepository.Verify(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WithTooLargeScreenshot_IsRejected()
    {
        var request = CreateValidRequest();
        request.Screenshot = CreatePngScreenshot(length: 3 * 1024 * 1024 + 1);

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain(error => error.Contains("3 MB or smaller", StringComparison.OrdinalIgnoreCase));
        _screenshotStorage.Verify(x => x.SaveAsync(It.IsAny<ContactScreenshotUpload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WithNonImageScreenshot_IsRejected()
    {
        var request = CreateValidRequest();
        request.Screenshot = new ContactScreenshotUpload
        {
            Content = new MemoryStream("not an image"u8.ToArray()),
            FileName = "notes.txt",
            ContentType = "text/plain",
            Length = 12
        };

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain(error => error.Contains("PNG, JPEG, or WebP", StringComparison.OrdinalIgnoreCase));
        _screenshotStorage.Verify(x => x.SaveAsync(It.IsAny<ContactScreenshotUpload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateContactMessageAsync_DefaultStatus_IsNew()
    {
        ContactMessage? savedMessage = null;
        _contactRepository.Setup(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMessage, CancellationToken>((message, _) => savedMessage = message)
            .Returns(Task.CompletedTask);

        await CreateService().CreateContactMessageAsync(CreateValidRequest());

        savedMessage.Should().NotBeNull();
        savedMessage!.Status.Should().Be(ContactMessageStatus.New);
    }

    [Fact]
    public async Task CreateContactMessageAsync_WhenScreenshotUploaded_ResponseIncludesScreenshotUrl()
    {
        _contactRepository.Setup(x => x.AddAsync(It.IsAny<ContactMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _screenshotStorage.Setup(x => x.SaveAsync(It.IsAny<ContactScreenshotUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactScreenshotStorageResult
            {
                Url = "uploads/contact-screenshots/uploaded.webp",
                FileName = "uploaded.webp",
                OriginalFileName = "issue.webp",
                ContentType = "image/webp",
                SizeBytes = 12
            });

        var request = CreateValidRequest();
        request.Screenshot = CreateWebpScreenshot();

        var response = await CreateService().CreateContactMessageAsync(request);

        response.Success.Should().BeTrue();
        response.Data!.ScreenshotUrl.Should().Be("uploads/contact-screenshots/uploaded.webp");
    }

    [Fact]
    public async Task GetContactMessagesAsync_ReturnsContactMessages()
    {
        _contactRepository.Setup(x => x.GetAsync(null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ContactMessage
                {
                    Id = Guid.NewGuid(),
                    Name = "Nadia",
                    Email = "nadia@example.com",
                    Subject = "Market issue",
                    Message = "Please check this message.",
                    Status = ContactMessageStatus.New,
                    CreatedAt = DateTime.UtcNow
                }
            ]);
        _contactRepository.Setup(x => x.CountAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var response = await CreateService().GetContactMessagesAsync(new ContactMessageSearchRequest());

        response.Data.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
        response.Data[0].Name.Should().Be("Nadia");
    }

    [Fact]
    public async Task GetContactMessagesAsync_PassesSearchAndStatusFilters()
    {
        _contactRepository.Setup(x => x.GetAsync("nadia", "Resolved", null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _contactRepository.Setup(x => x.CountAsync("nadia", "Resolved", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await CreateService().GetContactMessagesAsync(new ContactMessageSearchRequest
        {
            Search = "nadia",
            Status = "Resolved"
        });

        _contactRepository.Verify(x => x.GetAsync("nadia", "Resolved", null, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContactMessageAsync_ReturnsScreenshotUrlWhenAvailable()
    {
        var id = Guid.NewGuid();
        _contactRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactMessage
            {
                Id = id,
                Name = "Nadia",
                Email = "nadia@example.com",
                Subject = "Market issue",
                Message = "Please check this message.",
                ScreenshotUrl = "uploads/contact-screenshots/file.png",
                Status = ContactMessageStatus.Read
            });

        var response = await CreateService().GetContactMessageAsync(id);

        response.Success.Should().BeTrue();
        response.Data!.ScreenshotUrl.Should().Be("uploads/contact-screenshots/file.png");
    }

    [Fact]
    public async Task UpdateContactMessageStatusAsync_UpdatesStatusAndResolvedAt()
    {
        var id = Guid.NewGuid();
        var message = new ContactMessage { Id = id, Status = ContactMessageStatus.New };
        _contactRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var response = await CreateService().UpdateContactMessageStatusAsync(id, new UpdateContactMessageStatusRequest
        {
            Status = "Resolved"
        });

        response.Success.Should().BeTrue();
        message.Status.Should().Be(ContactMessageStatus.Resolved);
        message.ResolvedAt.Should().NotBeNull();
        _contactRepository.Verify(x => x.Update(message), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContactMessageNoteAsync_UpdatesAdminNote()
    {
        var id = Guid.NewGuid();
        var message = new ContactMessage { Id = id, Status = ContactMessageStatus.Read };
        _contactRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var response = await CreateService().UpdateContactMessageNoteAsync(id, new UpdateContactMessageNoteRequest
        {
            AdminNote = " Follow up tomorrow. "
        });

        response.Success.Should().BeTrue();
        message.AdminNote.Should().Be("Follow up tomorrow.");
        _contactRepository.Verify(x => x.Update(message), Times.Once);
    }

    private ContactService CreateService()
    {
        _requestContextAccessor.SetupGet(x => x.RawIpAddress).Returns("203.0.113.10");
        _requestContextAccessor.SetupGet(x => x.RawUserAgent).Returns("Test Browser");

        return new ContactService(
            _contactRepository.Object,
            _screenshotStorage.Object,
            _requestContextAccessor.Object,
            _unitOfWork.Object);
    }

    private static CreateContactMessageRequest CreateValidRequest()
    {
        return new CreateContactMessageRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Subject = "Issue with local price",
            Message = "I found an issue with a market price and want to report it."
        };
    }

    private static ContactScreenshotUpload CreatePngScreenshot(long? length = null)
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
        return new ContactScreenshotUpload
        {
            Content = new MemoryStream(bytes),
            FileName = "screen.png",
            ContentType = "image/png",
            Length = length ?? bytes.Length
        };
    }

    private static ContactScreenshotUpload CreateWebpScreenshot()
    {
        var bytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        return new ContactScreenshotUpload
        {
            Content = new MemoryStream(bytes),
            FileName = "issue.webp",
            ContentType = "image/webp",
            Length = bytes.Length
        };
    }
}
