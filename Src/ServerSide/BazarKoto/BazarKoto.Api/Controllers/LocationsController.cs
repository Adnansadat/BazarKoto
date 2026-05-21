using BazarKoto.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("divisions")]
    public async Task<IActionResult> GetDivisions([FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await _locationService.GetDivisionsAsync(search, cancellationToken));
    }

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] Guid divisionId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await _locationService.GetDistrictsAsync(divisionId, search, cancellationToken));
    }

    [HttpGet("upazilas")]
    public async Task<IActionResult> GetUpazilas([FromQuery] Guid districtId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await _locationService.GetUpazilasAsync(districtId, search, cancellationToken));
    }

    [HttpGet("unions-or-wards")]
    public async Task<IActionResult> GetUnionOrWards([FromQuery] Guid upazilaId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await _locationService.GetUnionOrWardsAsync(upazilaId, search, cancellationToken));
    }
}
