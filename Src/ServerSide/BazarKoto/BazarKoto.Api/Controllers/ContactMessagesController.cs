using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactMessagesController : ControllerBase
{
    private const int MaxMultipartRequestBytes = 4 * 1024 * 1024;

    private readonly IContactService _contactService;
    private readonly ILogger<ContactMessagesController> _logger;

    public ContactMessagesController(IContactService contactService, ILogger<ContactMessagesController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestBytes)]
    public async Task<IActionResult> CreateContactMessage(CancellationToken cancellationToken)
    {
        try
        {
            if (Request.ContentLength > MaxMultipartRequestBytes)
            {
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", ["Request size must be 4 MB or smaller."]));
            }

            var form = await Request.ReadFormAsync(cancellationToken);
            var screenshotFiles = form.Files.GetFiles("screenshot");

            if (screenshotFiles.Count > 1)
            {
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", ["Only one screenshot file can be uploaded."]));
            }

            if (form.Files.Count != screenshotFiles.Count)
            {
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", ["Only the screenshot file field is supported."]));
            }

            var screenshot = screenshotFiles.Count == 1 ? screenshotFiles[0] : null;
            await using var screenshotStream = screenshot?.OpenReadStream();
            var response = await _contactService.CreateContactMessageAsync(new CreateContactMessageRequest
            {
                Name = form["name"].ToString(),
                Email = form["email"].ToString(),
                Subject = form["subject"].ToString(),
                Message = form["message"].ToString(),
                Screenshot = screenshot is null || screenshotStream is null
                    ? null
                    : new ContactScreenshotUpload
                    {
                        Content = screenshotStream,
                        FileName = screenshot.FileName,
                        ContentType = screenshot.ContentType,
                        Length = screenshot.Length
                    }
            }, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(response.Message, response.Errors));
            }

            return Ok(new
            {
                success = true,
                message = response.Message,
                id = response.Data!.Id
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Contact message submission failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Request failed.", ["Unable to submit your message right now."]));
        }
    }
}
