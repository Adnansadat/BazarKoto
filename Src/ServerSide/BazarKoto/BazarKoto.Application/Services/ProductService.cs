using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Products;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IReadOnlyList<ProductCategoryResponse>>> GetProductCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _productRepository.GetCategoriesAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<ProductCategoryResponse>>.Ok(categories.Select(ToCategoryResponse).ToList());
    }

    public async Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAsync(request.CategoryId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _productRepository.CountAsync(request.CategoryId, request.Search, cancellationToken);
        return Page(products.Select(ToResponse), request, totalCount);
    }

    public async Task<ApiResponse<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            CategoryId = request.CategoryId,
            NameEn = request.NameEn,
            NameBn = request.NameBn,
            LocalName = request.LocalName,
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.NameEn) : Slugify(request.Slug),
            PrimaryUnit = request.PrimaryUnit,
            ProductState = ParseEnum(request.ProductState, ProductState.Fresh),
            Notes = request.Notes,
            Status = RecordStatus.Pending,
            IsActive = true
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductResponse>.Ok(ToResponse(product), "Product submitted successfully.");
    }

    public async Task<PagedResponse<ProductResponse>> GetDuplicateProductsAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetDuplicatesAsync(request.NameEn, cancellationToken);
        return Page(products.Select(ToResponse), new PaginationRequest());
    }

    public Task<ApiResponse<ProductResponse>> UpdateProductAsync(Guid id, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<ProductResponse>.Fail("Product persistence is not configured yet."));
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryNameEn = product.Category?.NameEn ?? string.Empty,
            CategoryNameBn = product.Category?.NameBn ?? string.Empty,
            NameEn = product.NameEn,
            NameBn = product.NameBn,
            LocalName = product.LocalName,
            Slug = product.Slug,
            PrimaryUnit = product.PrimaryUnit,
            ProductState = product.ProductState.ToString(),
            Notes = product.Notes,
            Status = product.Status.ToString(),
            IsActive = product.IsActive
        };
    }

    private static ProductCategoryResponse ToCategoryResponse(ProductCategory category)
    {
        return new ProductCategoryResponse
        {
            Id = category.Id,
            NameEn = category.NameEn,
            NameBn = category.NameBn,
            Slug = category.Slug,
            DescriptionEn = category.DescriptionEn,
            DescriptionBn = category.DescriptionBn,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    private static PagedResponse<T> Page<T>(IEnumerable<T> items, PaginationRequest request, int? totalCount = null)
    {
        var list = items.ToList();
        var count = totalCount ?? list.Count;
        var pageItems = totalCount.HasValue ? list : list.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResponse<T>
        {
            Message = "Success",
            Data = pageItems,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var characters = normalized.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
