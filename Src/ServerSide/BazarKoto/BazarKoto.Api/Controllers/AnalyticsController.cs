using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpPost("page-visit")]
    public async Task<IActionResult> TrackPageVisit(TrackPageVisitRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _analyticsService.TrackPageVisitAsync(request, cancellationToken));
    }
}
