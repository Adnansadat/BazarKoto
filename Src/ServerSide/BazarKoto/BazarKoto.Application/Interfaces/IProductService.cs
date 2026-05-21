using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Products;

namespace BazarKoto.Application.Interfaces;

public interface IProductService
{
    Task<ApiResponse<IReadOnlyList<ProductCategoryResponse>>> GetProductCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ProductResponse>> GetDuplicateProductsAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductResponse>> UpdateProductAsync(Guid id, CreateProductRequest request, CancellationToken cancellationToken = default);
}
