using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactMessagesController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactMessagesController> _logger;

    public ContactMessagesController(IContactService contactService, ILogger<ContactMessagesController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> CreateContactMessage([FromForm] ContactMessageFormRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (HttpContext.Request.Form.Files.Count > 1)
            {
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", ["Only one screenshot file can be uploaded."]));
            }

            if (HttpContext.Request.Form.Files.Count == 1 && request.Screenshot is null)
            {
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", ["Only the screenshot file field is supported."]));
            }

            await using var screenshotStream = request.Screenshot?.OpenReadStream();
            var response = await _contactService.CreateContactMessageAsync(new CreateContactMessageRequest
            {
                Name = request.Name,
                Email = request.Email,
                Subject = request.Subject,
                Message = request.Message,
                Screenshot = request.Screenshot is null || screenshotStream is null
                    ? null
                    : new ContactScreenshotUpload
                    {
                        Content = screenshotStream,
                        FileName = request.Screenshot.FileName,
                        ContentType = request.Screenshot.ContentType,
                        Length = request.Screenshot.Length
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

public class ContactMessageFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IFormFile? Screenshot { get; set; }
}
