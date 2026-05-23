using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAsync(Guid? categoryId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid? categoryId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetDuplicatesAsync(Guid categoryId, string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCategoryAndSlugAsync(Guid categoryId, string slug, Guid? excludeProductId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
}
