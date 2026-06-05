using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Prices;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public PricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPrices([FromQuery] PriceSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetPricesAsync(request, cancellationToken));
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestPrice([FromQuery] PriceSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetLatestPriceAsync(request, cancellationToken));
    }

    [HttpGet("home-preview")]
    public async Task<IActionResult> GetHomePricePreview(CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetHomePricePreviewAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> SubmitPrice(SubmitPriceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.SubmitPriceAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePrice(Guid id, UpdatePriceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.UpdatePriceAsync(id, request, cancellationToken));
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayPrices([FromQuery] PriceSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetTodayPricesAsync(request, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetPriceSummary([FromQuery] PriceSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetPriceSummaryAsync(request, cancellationToken));
    }
}
