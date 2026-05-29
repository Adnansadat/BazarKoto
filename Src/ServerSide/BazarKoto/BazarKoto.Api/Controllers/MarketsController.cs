using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Markets;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketsController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketsController(IMarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMarkets([FromQuery] MarketSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.GetMarketsAsync(request, cancellationToken));
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetMarketOptions([FromQuery] MarketSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.GetMarketOptionsAsync(request, cancellationToken));
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyMarkets([FromQuery] MarketSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.GetNearbyMarketsAsync(request, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateMarket(CreateMarketRequest request, CancellationToken cancellationToken)
    {
        var response = await _marketService.CreateMarketAsync(request, cancellationToken);

        return response.Success ? Ok(response) : Conflict(response);
    }
}
