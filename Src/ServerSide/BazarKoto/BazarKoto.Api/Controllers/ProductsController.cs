using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Products;
using Microsoft.AspNetCore.Mvc;

namespace BazarKoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetProductsAsync(request, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.CreateProductAsync(request, cancellationToken));
    }
}
