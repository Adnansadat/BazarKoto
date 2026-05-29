using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Prices;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductPricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public ProductPricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductPrices([FromQuery] PublicProductPriceSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetPublicProductPricesAsync(request, cancellationToken));
    }
}
