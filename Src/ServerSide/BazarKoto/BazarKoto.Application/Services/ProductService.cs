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

    public async Task<PagedResponse<ProductOptionResponse>> GetProductOptionsAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetOptionsAsync(request.CategoryId, request.Search, request.UnionOrWardId, request.MarketId, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _productRepository.CountOptionsAsync(request.CategoryId, request.Search, request.UnionOrWardId, request.MarketId, cancellationToken);
        return Page(products.Select(ToOptionResponse), request, totalCount);
    }

    public async Task<ApiResponse<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<ProductResponse>.Fail("Validation failed.", validationErrors);
        }

        var category = await _productRepository.GetCategoryByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            return ApiResponse<ProductResponse>.Fail("Product category was not found.");
        }

        if (!TryParseEnum<ProductState>(request.ProductState, out var productState))
        {
            return ApiResponse<ProductResponse>.Fail("Product state is not valid.");
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.NameEn) : Slugify(request.Slug);

        if (await _productRepository.ExistsByCategoryAndSlugAsync(request.CategoryId, slug, null, cancellationToken))
        {
            return ApiResponse<ProductResponse>.Fail("This product already exists in the selected category.");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            NameEn = request.NameEn.Trim(),
            NameBn = request.NameBn.Trim(),
            LocalName = NormalizeOptional(request.LocalName),
            Slug = slug,
            PrimaryUnit = request.PrimaryUnit.Trim(),
            ProductState = productState,
            Notes = NormalizeOptional(request.Notes),
            Status = RecordStatus.Pending,
            IsActive = true
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        product.Category = category;

        return ApiResponse<ProductResponse>.Ok(ToResponse(product), "Product submitted successfully.");
    }

    public async Task<PagedResponse<ProductResponse>> GetDuplicateProductsAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.NameEn) : Slugify(request.Slug);
        IReadOnlyList<Product> products = request.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(slug)
            ? Array.Empty<Product>()
            : await _productRepository.GetDuplicatesAsync(request.CategoryId, slug, cancellationToken);
        return Page(products.Select(ToResponse), new PaginationRequest());
    }

    public async Task<ApiResponse<ProductResponse>> UpdateProductAsync(Guid id, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ApiResponse<ProductResponse>.Fail("Product was not found.");
        }

        var validationErrors = ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<ProductResponse>.Fail("Validation failed.", validationErrors);
        }

        var category = await _productRepository.GetCategoryByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            return ApiResponse<ProductResponse>.Fail("Product category was not found.");
        }

        if (!TryParseEnum<ProductState>(request.ProductState, out var productState))
        {
            return ApiResponse<ProductResponse>.Fail("Product state is not valid.");
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.NameEn) : Slugify(request.Slug);

        if (await _productRepository.ExistsByCategoryAndSlugAsync(request.CategoryId, slug, id, cancellationToken))
        {
            return ApiResponse<ProductResponse>.Fail("This product already exists in the selected category.");
        }

        product.CategoryId = request.CategoryId;
        product.NameEn = request.NameEn.Trim();
        product.NameBn = request.NameBn.Trim();
        product.LocalName = NormalizeOptional(request.LocalName);
        product.Slug = slug;
        product.PrimaryUnit = request.PrimaryUnit.Trim();
        product.ProductState = productState;
        product.Notes = NormalizeOptional(request.Notes);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        product.Category = category;

        return ApiResponse<ProductResponse>.Ok(ToResponse(product), "Product updated successfully.");
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

    private static ProductOptionResponse ToOptionResponse(Product product)
    {
        return new ProductOptionResponse
        {
            Id = product.Id,
            ProductId = product.Id,
            ProductNameEn = product.NameEn,
            ProductNameBn = product.NameBn,
            LocalOrAlternateName = product.LocalName,
            CategoryId = product.CategoryId,
            CategoryNameEn = product.Category?.NameEn ?? string.Empty,
            CategoryNameBn = product.Category?.NameBn ?? string.Empty,
            PrimaryUnit = product.PrimaryUnit,
            ProductState = product.ProductState.ToString(),
            DisplayLabel = string.IsNullOrWhiteSpace(product.Category?.NameEn)
                ? $"{product.NameEn} ({product.PrimaryUnit})"
                : $"{product.NameEn} — {product.Category.NameEn}"
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

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed)
        where TEnum : struct
    {
        return Enum.TryParse(value, true, out parsed);
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var characters = normalized.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static List<string> ValidateRequest(CreateProductRequest request)
    {
        var errors = new List<string>();

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add("Product Category is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NameEn))
        {
            errors.Add("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NameBn))
        {
            errors.Add("Local or alternate name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryUnit))
        {
            errors.Add("Primary unit is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ProductState))
        {
            errors.Add("Product state is required.");
        }

        return errors;
    }
}
