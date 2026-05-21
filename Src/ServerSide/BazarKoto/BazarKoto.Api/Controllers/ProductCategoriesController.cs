using BazarKoto.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/product-categories")]
public class ProductCategoriesController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductCategoriesController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductCategories(CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetProductCategoriesAsync(cancellationToken));
    }
}
