using BazarKoto.Api.Services;
using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;
using BazarKoto.Contracts.Markets;
using BazarKoto.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IMarketService _marketService;
    private readonly IProductService _productService;
    private readonly IPriceService _priceService;
    private readonly IContactService _contactService;
    private readonly IWebHostEnvironment _environment;

    public AdminController(
        IAdminDashboardService dashboardService,
        IAnalyticsService analyticsService,
        IMarketService marketService,
        IProductService productService,
        IPriceService priceService,
        IContactService contactService,
        IWebHostEnvironment environment)
    {
        _dashboardService = dashboardService;
        _analyticsService = analyticsService;
        _marketService = marketService;
        _productService = productService;
        _priceService = priceService;
        _contactService = contactService;
        _environment = environment;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetDashboardAsync(cancellationToken));
    }

    [HttpGet("traffic")]
    public async Task<IActionResult> GetTraffic(CancellationToken cancellationToken)
    {
        return Ok(await _analyticsService.GetTrafficSummaryAsync(cancellationToken));
    }

    [HttpGet("peak-hours")]
    public async Task<IActionResult> GetPeakHours(CancellationToken cancellationToken)
    {
        return Ok(await _analyticsService.GetPeakHoursAsync(cancellationToken));
    }

    [HttpGet("ad-readiness")]
    public async Task<IActionResult> GetAdReadiness(CancellationToken cancellationToken)
    {
        return Ok(await _analyticsService.GetAdReadinessAsync(cancellationToken));
    }

    [HttpGet("markets/pending")]
    public async Task<IActionResult> GetPendingMarkets([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.GetPendingMarketsAsync(request, cancellationToken));
    }

    [HttpPut("markets/{id:guid}")]
    public async Task<IActionResult> UpdateMarket(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.UpdateMarketAsync(id, request, cancellationToken));
    }

    [HttpDelete("markets/{id:guid}")]
    public async Task<IActionResult> DeleteMarket(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _marketService.DeleteMarketAsync(id, cancellationToken));
    }

    [HttpGet("products/duplicates")]
    public async Task<IActionResult> GetDuplicateProducts([FromQuery] CreateProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetDuplicateProductsAsync(request, cancellationToken));
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, CreateProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.UpdateProductAsync(id, request, cancellationToken));
    }

    [HttpGet("prices/pending")]
    public async Task<IActionResult> GetPendingPrices([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.GetPendingPricesAsync(request, cancellationToken));
    }

    [HttpPut("prices/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePrice(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.ApprovePriceAsync(id, cancellationToken));
    }

    [HttpPut("prices/{id:guid}/reject")]
    public async Task<IActionResult> RejectPrice(Guid id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        return Ok(await _priceService.RejectPriceAsync(id, reason, cancellationToken));
    }

    [HttpGet("ContactMessages")]
    public async Task<IActionResult> GetContactMessages([FromQuery] ContactMessageSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _contactService.GetContactMessagesAsync(request, cancellationToken));
    }

    [HttpGet("ContactMessages/{id:guid}")]
    public async Task<IActionResult> GetContactMessage(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _contactService.GetContactMessageAsync(id, cancellationToken));
    }

    [HttpGet("ContactMessages/{id:guid}/screenshot")]
    public async Task<IActionResult> GetContactMessageScreenshot(Guid id, CancellationToken cancellationToken)
    {
        var response = await _contactService.GetContactMessageAsync(id, cancellationToken);

        if (!response.Success || response.Data is null)
        {
            return NotFound(response);
        }

        if (string.IsNullOrWhiteSpace(response.Data.ScreenshotFileName))
        {
            return NotFound(ApiResponse<object>.Fail("Screenshot was not found."));
        }

        var uploadRoot = ContactScreenshotStorage.GetUploadRoot(_environment);
        var filePath = Path.GetFullPath(Path.Combine(uploadRoot, response.Data.ScreenshotFileName));

        if (!filePath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(filePath))
        {
            return NotFound(ApiResponse<object>.Fail("Screenshot was not found."));
        }

        return PhysicalFile(filePath, response.Data.ScreenshotContentType ?? "application/octet-stream");
    }

    [HttpPatch("ContactMessages/{id:guid}/status")]
    public async Task<IActionResult> UpdateContactMessageStatus(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _contactService.UpdateContactMessageStatusAsync(id, request, cancellationToken));
    }

    [HttpPatch("ContactMessages/{id:guid}/note")]
    public async Task<IActionResult> UpdateContactMessageNote(Guid id, UpdateContactMessageNoteRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _contactService.UpdateContactMessageNoteAsync(id, request, cancellationToken));
    }
}
