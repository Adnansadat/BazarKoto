using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Contact;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactController(IContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateContactMessage(CreateContactMessageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _contactService.CreateContactMessageAsync(request, cancellationToken));
    }
}
