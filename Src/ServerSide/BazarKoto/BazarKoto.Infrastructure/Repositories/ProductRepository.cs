using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public ProductRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductCategories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.NameEn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAsync(Guid? categoryId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(categoryId, search);

        return await LoadPageWithCategoryAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public Task<int> CountAsync(Guid? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        return BuildQuery(categoryId, search).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetOptionsAsync(Guid? categoryId = null, string? search = null, Guid? unionOrWardId = null, Guid? marketId = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = BuildOptionQuery(categoryId, search, unionOrWardId, marketId);

        return await LoadPageWithCategoryAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public Task<int> CountOptionsAsync(Guid? categoryId = null, string? search = null, Guid? unionOrWardId = null, Guid? marketId = null, CancellationToken cancellationToken = default)
    {
        return BuildOptionQuery(categoryId, search, unionOrWardId, marketId).CountAsync(cancellationToken);
    }

    public Task<int> CountByStatusAsync(RecordStatus status, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.Status == status)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountDistinctCategoriesAsync(RecordStatus status, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.Status == status)
            .Select(x => x.CategoryId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
    }

    private IQueryable<Product> BuildQuery(Guid? categoryId, string? search)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.Status == RecordStatus.Approved)
            .AsQueryable();

        return ApplyCategoryAndSearchFilters(query, categoryId, search);
    }

    private IQueryable<Product> BuildOptionQuery(Guid? categoryId, string? search, Guid? unionOrWardId, Guid? marketId)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && (x.Status == RecordStatus.Approved || x.Status == RecordStatus.Pending))
            .AsQueryable();

        if (marketId.HasValue)
        {
            query = query.Where(product => _dbContext.PriceSubmissions.Any(price =>
                price.Status == SubmissionStatus.Approved &&
                price.ProductId == product.Id &&
                price.MarketId == marketId.Value));
        }
        else if (unionOrWardId.HasValue)
        {
            query = query.Where(product => _dbContext.PriceSubmissions.Any(price =>
                price.Status == SubmissionStatus.Approved &&
                price.ProductId == product.Id &&
                ((price.UnionOrWardId.HasValue && price.UnionOrWardId == unionOrWardId.Value) ||
                 (!price.UnionOrWardId.HasValue && price.Market != null && price.Market.UnionOrWardId == unionOrWardId.Value))));
        }

        return ApplyCategoryAndSearchFilters(query, categoryId, search);
    }

    private async Task<IReadOnlyList<Product>> LoadPageWithCategoryAsync(IQueryable<Product> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var pageIds = await query
            .OrderBy(x => x.NameEn)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
        {
            return [];
        }

        var orderById = pageIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return products
            .OrderBy(x => orderById[x.Id])
            .ToList();
    }

    private static IQueryable<Product> ApplyCategoryAndSearchFilters(IQueryable<Product> query, Guid? categoryId, string? search)
    {
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x =>
                x.NameEn.Contains(normalizedSearch) ||
                x.NameBn.Contains(normalizedSearch) ||
                (x.LocalName != null && x.LocalName.Contains(normalizedSearch)) ||
                x.Slug.Contains(normalizedSearch));
        }

        return query;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetDuplicatesAsync(Guid categoryId, string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLower();

        return await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x =>
                x.CategoryId == categoryId &&
                x.Slug.ToLower() == normalizedSlug)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCategoryAndSlugAsync(Guid categoryId, string slug, Guid? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLower();

        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(x =>
                x.CategoryId == categoryId &&
                x.Slug.ToLower() == normalizedSlug &&
                (!excludeProductId.HasValue || x.Id != excludeProductId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _dbContext.Products.Update(product);
    }
}
