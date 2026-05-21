using System.Text.Json;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BazarKoto.Infrastructure.Persistence.Seed.MasterData;

public class MasterDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BazarKotoDbContext _dbContext;
    private readonly ILogger<MasterDataSeeder> _logger;

    public MasterDataSeeder(BazarKotoDbContext dbContext, ILogger<MasterDataSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(string masterDataPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Master data seed path: {MasterDataPath}", masterDataPath);

        if (!Directory.Exists(masterDataPath))
        {
            _logger.LogWarning("Master data folder was not found: {MasterDataPath}", masterDataPath);
            return;
        }

        await SeedDivisionsAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedDistrictsAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedUpazilasAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedUnionsOrWardsAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedProductCategoriesAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SeedProductsAsync(masterDataPath, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDivisionsAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadRequiredAsync<DivisionSeedDto>(masterDataPath, "divisions.json", cancellationToken);
        var inserted = 0;

        foreach (var row in rows)
        {
            var division = await _dbContext.Divisions.FirstOrDefaultAsync(x => x.Slug == row.Slug, cancellationToken);

            if (division is null)
            {
                _dbContext.Divisions.Add(new Division
                {
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    BbsCode = row.BbsCode,
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted divisions count: {InsertedCount}", inserted);
    }

    private async Task SeedDistrictsAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadRequiredAsync<DistrictSeedDto>(masterDataPath, "districts.json", cancellationToken);
        var inserted = 0;
        var skippedMissingParent = 0;

        foreach (var row in rows)
        {
            var division = await _dbContext.Divisions.FirstOrDefaultAsync(x => x.Slug == row.DivisionSlug, cancellationToken);

            if (division is null)
            {
                skippedMissingParent++;
                _logger.LogWarning("Skipping district {Slug}; parent division {DivisionSlug} was not found.", row.Slug, row.DivisionSlug);
                continue;
            }

            var district = await _dbContext.Districts.FirstOrDefaultAsync(x => x.DivisionId == division.Id && x.Slug == row.Slug, cancellationToken);

            if (district is null)
            {
                _dbContext.Districts.Add(new District
                {
                    DivisionId = division.Id,
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    BbsCode = row.BbsCode,
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted districts count: {InsertedCount}", inserted);
        _logger.LogInformation("Skipped districts count due to missing parent: {SkippedCount}", skippedMissingParent);
    }

    private async Task SeedUpazilasAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadRequiredAsync<UpazilaSeedDto>(masterDataPath, "upazilas.json", cancellationToken);
        var inserted = 0;
        var skippedMissingParent = 0;

        foreach (var row in rows)
        {
            var district = await _dbContext.Districts.FirstOrDefaultAsync(x => x.Slug == row.DistrictSlug, cancellationToken);

            if (district is null)
            {
                skippedMissingParent++;
                _logger.LogWarning("Skipping upazila {Slug}; parent district {DistrictSlug} was not found.", row.Slug, row.DistrictSlug);
                continue;
            }

            var upazila = await _dbContext.Upazilas.FirstOrDefaultAsync(x => x.DistrictId == district.Id && x.Slug == row.Slug, cancellationToken);

            if (upazila is null)
            {
                _dbContext.Upazilas.Add(new Upazila
                {
                    DistrictId = district.Id,
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    BbsCode = row.BbsCode,
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted upazilas count: {InsertedCount}", inserted);
        _logger.LogInformation("Skipped upazilas count due to missing parent: {SkippedCount}", skippedMissingParent);
    }

    private async Task SeedUnionsOrWardsAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadOptionalAsync<UnionOrWardSeedDto>(masterDataPath, "unions-or-wards.json", cancellationToken);
        var inserted = 0;
        var skippedMissingParent = 0;

        foreach (var row in rows)
        {
            var upazila = await _dbContext.Upazilas.FirstOrDefaultAsync(x => x.Slug == row.UpazilaSlug, cancellationToken);

            if (upazila is null)
            {
                skippedMissingParent++;
                _logger.LogWarning("Skipping union/ward {Slug}; parent upazila {UpazilaSlug} was not found.", row.Slug, row.UpazilaSlug);
                continue;
            }

            var unionOrWard = await _dbContext.UnionOrWards.FirstOrDefaultAsync(x => x.UpazilaId == upazila.Id && x.Slug == row.Slug, cancellationToken);

            if (unionOrWard is null)
            {
                _dbContext.UnionOrWards.Add(new UnionOrWard
                {
                    UpazilaId = upazila.Id,
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    BbsCode = row.BbsCode,
                    Type = ParseEnum(row.Type, UnionOrWardType.Unknown),
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted unions/wards count: {InsertedCount}", inserted);
        _logger.LogInformation("Skipped unions/wards count due to missing parent: {SkippedCount}", skippedMissingParent);
    }

    private async Task SeedProductCategoriesAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadRequiredAsync<ProductCategorySeedDto>(masterDataPath, "product-categories.json", cancellationToken);
        var inserted = 0;

        foreach (var row in rows)
        {
            var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Slug == row.Slug, cancellationToken);

            if (category is null)
            {
                _dbContext.ProductCategories.Add(new ProductCategory
                {
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    DescriptionEn = row.DescriptionEn,
                    DescriptionBn = row.DescriptionBn,
                    SortOrder = row.SortOrder,
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted product categories count: {InsertedCount}", inserted);
    }

    private async Task SeedProductsAsync(string masterDataPath, CancellationToken cancellationToken)
    {
        var rows = await ReadRequiredAsync<ProductSeedDto>(masterDataPath, "products.json", cancellationToken);
        var inserted = 0;
        var skippedMissingParent = 0;

        foreach (var row in rows)
        {
            var category = await _dbContext.ProductCategories.FirstOrDefaultAsync(x => x.Slug == row.CategorySlug, cancellationToken);

            if (category is null)
            {
                skippedMissingParent++;
                _logger.LogWarning("Skipping product {Slug}; parent category {CategorySlug} was not found.", row.Slug, row.CategorySlug);
                continue;
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.CategoryId == category.Id && x.Slug == row.Slug, cancellationToken);

            if (product is null)
            {
                _dbContext.Products.Add(new Product
                {
                    CategoryId = category.Id,
                    NameEn = row.NameEn,
                    NameBn = row.NameBn,
                    Slug = row.Slug,
                    PrimaryUnit = row.PrimaryUnit,
                    ProductState = ParseEnum(row.ProductState, ProductState.Fresh),
                    Notes = row.Notes,
                    Status = RecordStatus.Approved,
                    IsActive = true
                });
                inserted++;
            }
        }

        _logger.LogInformation("Inserted products count: {InsertedCount}", inserted);
        _logger.LogInformation("Skipped products count due to missing parent: {SkippedCount}", skippedMissingParent);
    }

    private async Task<IReadOnlyList<T>> ReadRequiredAsync<T>(string masterDataPath, string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(masterDataPath, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("{FileName} found: false", fileName);
            _logger.LogWarning("Required master data file is missing: {FilePath}", filePath);
            return [];
        }

        _logger.LogInformation("{FileName} found: true", fileName);
        return await ReadJsonAsync<T>(filePath, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> ReadOptionalAsync<T>(string masterDataPath, string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(masterDataPath, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("{FileName} found: false", fileName);
            _logger.LogWarning("Optional master data file is missing: {FilePath}", filePath);
            return [];
        }

        _logger.LogInformation("{FileName} found: true", fileName);
        return await ReadJsonAsync<T>(filePath, cancellationToken);
    }

    private static async Task<IReadOnlyList<T>> ReadJsonAsync<T>(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private class DivisionSeedDto
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? BbsCode { get; set; }
    }

    private sealed class DistrictSeedDto : DivisionSeedDto
    {
        public string DivisionSlug { get; set; } = string.Empty;
    }

    private sealed class UpazilaSeedDto : DivisionSeedDto
    {
        public string DistrictSlug { get; set; } = string.Empty;
    }

    private sealed class UnionOrWardSeedDto : DivisionSeedDto
    {
        public string UpazilaSlug { get; set; } = string.Empty;
        public string Type { get; set; } = UnionOrWardType.Unknown.ToString();
    }

    private sealed class ProductCategorySeedDto
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionBn { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed class ProductSeedDto
    {
        public string CategorySlug { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PrimaryUnit { get; set; } = string.Empty;
        public string ProductState { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
