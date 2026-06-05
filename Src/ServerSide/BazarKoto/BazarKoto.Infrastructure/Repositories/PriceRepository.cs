using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Features.Prices;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public PriceRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PriceSubmission>> GetAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        SubmissionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            Query().AsNoTracking(),
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date,
            status);

        return await query.OrderByDescending(x => x.PriceDate).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        SubmissionStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            _dbContext.PriceSubmissions.AsNoTracking(),
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date,
            status);

        var totalCount = await query.CountAsync(cancellationToken);
        var skippedCount = (pageNumber - 1) * pageSize;
        var pageIds = await query
            .OrderByDescending(x => x.PriceDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skippedCount)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
        {
            return ([], totalCount);
        }

        var orderById = pageIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var items = await Query()
            .AsNoTracking()
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        items = items.OrderBy(x => orderById[x.Id]).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<PriceSubmission>> GetPublicProductPricesAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GetPublicProductPricesPageAsync(
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date,
            search,
            pageNumber: 1,
            pageSize: int.MaxValue,
            cancellationToken);

        return items;
    }

    public async Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPublicProductPricesPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyPublicProductPriceFilters(
            _dbContext.PriceSubmissions.AsNoTracking().Where(x => x.Status == SubmissionStatus.Approved),
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date,
            search);

        var totalCount = await query.CountAsync(cancellationToken);
        var skippedCount = (pageNumber - 1) * pageSize;
        var pageIds = await query
            .OrderByDescending(x => x.PriceDate)
            .ThenByDescending(x => x.PriceTime)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skippedCount)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
        {
            return ([], totalCount);
        }

        var orderById = pageIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var items = await Query()
            .AsNoTracking()
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        items = items.OrderBy(x => orderById[x.Id]).ToList();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetAdminPricesAsync(
        string? search = null,
        SubmissionStatus? status = null,
        DateTime? submittedFrom = null,
        DateTime? submittedTo = null,
        Guid? productId = null,
        Guid? marketId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceSubmissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(x =>
                (x.Product != null && (
                    x.Product.NameEn.Contains(normalizedSearch) ||
                    x.Product.NameBn.Contains(normalizedSearch) ||
                    (x.Product.LocalName != null && x.Product.LocalName.Contains(normalizedSearch)))) ||
                (x.Market != null && (
                    x.Market.MarketName.Contains(normalizedSearch) ||
                    x.Market.Area.Contains(normalizedSearch) ||
                    x.Market.VillageOrMoholla.Contains(normalizedSearch) ||
                    x.Market.Landmark.Contains(normalizedSearch))) ||
                x.Unit.Contains(normalizedSearch));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (submittedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= submittedFrom.Value);
        }

        if (submittedTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= submittedTo.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (marketId.HasValue)
        {
            query = query.Where(x => x.MarketId == marketId.Value);
        }

        var pageIds = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.PriceDate)
            .ThenByDescending(x => x.PriceTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var skippedCount = (pageNumber - 1) * pageSize;
        var totalCount = pageIds.Count < pageSize
            ? skippedCount + pageIds.Count
            : await query.CountAsync(cancellationToken);

        if (pageIds.Count == 0)
        {
            return ([], totalCount);
        }

        var orderById = pageIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var items = await Query()
            .AsNoTracking()
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        items = items.OrderBy(x => orderById[x.Id]).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<PriceSubmission>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await Query().AsNoTracking().Where(x => x.Status == SubmissionStatus.Pending).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(SubmissionStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceSubmissions.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPendingPageAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceSubmissions
            .AsNoTracking()
            .Where(x => x.Status == SubmissionStatus.Pending);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageIds = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.PriceDate)
            .ThenByDescending(x => x.PriceTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
        {
            return ([], totalCount);
        }

        var orderById = pageIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var items = await Query()
            .AsNoTracking()
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        items = items.OrderBy(x => orderById[x.Id]).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<HomePricePreviewItem>> GetHomePricePreviewAsync(
        int limit = 60,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PriceSubmissions
            .AsNoTracking()
            .Where(x => x.Status == SubmissionStatus.Approved && x.PricePerUnit > 0);

        return await query
            .Where(latest => latest.Id == query
                .Where(candidate =>
                    candidate.ProductId == latest.ProductId &&
                    candidate.MarketId == latest.MarketId &&
                    candidate.Unit == latest.Unit)
                .OrderByDescending(candidate => candidate.PriceDate)
                .ThenByDescending(candidate => candidate.PriceTime)
                .ThenByDescending(candidate => candidate.CreatedAt)
                .ThenByDescending(candidate => candidate.Id)
                .Select(candidate => candidate.Id)
                .FirstOrDefault())
            .Select(latest => new HomePricePreviewItem
            {
                MarketName = latest.Market != null ? latest.Market.MarketName : string.Empty,
                ProductNameEn = latest.Product != null ? latest.Product.NameEn : string.Empty,
                ProductNameBn = latest.Product != null ? latest.Product.NameBn : string.Empty,
                CategoryNameEn = latest.Product != null && latest.Product.Category != null ? latest.Product.Category.NameEn : string.Empty,
                CategoryNameBn = latest.Product != null && latest.Product.Category != null ? latest.Product.Category.NameBn : string.Empty,
                Unit = latest.Unit,
                PricePerUnit = latest.PricePerUnit,
                PreviousPricePerUnit = query
                    .Where(previous =>
                        previous.ProductId == latest.ProductId &&
                        previous.MarketId == latest.MarketId &&
                        previous.Unit == latest.Unit &&
                        previous.Id != latest.Id)
                    .OrderByDescending(previous => previous.PriceDate)
                    .ThenByDescending(previous => previous.PriceTime)
                    .ThenByDescending(previous => previous.CreatedAt)
                    .ThenByDescending(previous => previous.Id)
                    .Select(previous => (decimal?)previous.PricePerUnit)
                    .FirstOrDefault()
            })
            .OrderBy(x => x.ProductNameEn)
            .ThenBy(x => x.MarketName)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DailyPriceSummary>> GetDailySummaryAggregatesAsync(
        DateOnly date,
        SubmissionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PriceSubmissions
            .AsNoTracking()
            .Where(x => x.PriceDate == date && x.Status == status && x.Market != null)
            .GroupBy(x => new
            {
                x.ProductId,
                x.MarketId,
                x.Market!.DivisionId,
                x.Market.DistrictId,
                x.Market.UpazilaId,
                x.Market.UnionOrWardId,
                x.PriceDate
            })
            .Select(group => new DailyPriceSummary
            {
                ProductId = group.Key.ProductId,
                MarketId = group.Key.MarketId,
                DivisionId = group.Key.DivisionId,
                DistrictId = group.Key.DistrictId,
                UpazilaId = group.Key.UpazilaId,
                UnionOrWardId = group.Key.UnionOrWardId,
                PriceDate = group.Key.PriceDate,
                MinPrice = group.Min(x => x.PricePerUnit),
                MaxPrice = group.Max(x => x.PricePerUnit),
                AveragePrice = group.Average(x => x.PricePerUnit),
                SubmissionCount = group.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceSubmission>> GetTodayAsync(DateOnly date, SubmissionStatus? status = null, CancellationToken cancellationToken = default)
    {
        return await GetAsync(date: date, status: status, cancellationToken: cancellationToken);
    }

    public Task<PriceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(PriceSubmission priceSubmission, CancellationToken cancellationToken = default)
    {
        await _dbContext.PriceSubmissions.AddAsync(priceSubmission, cancellationToken);
    }

    public void Update(PriceSubmission priceSubmission)
    {
        _dbContext.PriceSubmissions.Update(priceSubmission);
    }

    private IQueryable<PriceSubmission> Query()
    {
        return _dbContext.PriceSubmissions
            .Include(x => x.Market)
            .ThenInclude(x => x!.Division)
            .Include(x => x.Market)
            .ThenInclude(x => x!.District)
            .Include(x => x.Market)
            .ThenInclude(x => x!.Upazila)
            .Include(x => x.Market)
            .ThenInclude(x => x!.UnionOrWard)
            .Include(x => x.Product)
            .ThenInclude(x => x!.Category)
            .Include(x => x.SubmittedByUser);
    }

    private static IQueryable<PriceSubmission> ApplyPublicProductPriceFilters(
        IQueryable<PriceSubmission> query,
        Guid? divisionId,
        Guid? districtId,
        Guid? upazilaId,
        Guid? unionOrWardId,
        Guid? marketId,
        Guid? categoryId,
        Guid? productId,
        DateOnly? date,
        string? search)
    {
        if (marketId.HasValue)
        {
            query = query.Where(x => x.MarketId == marketId.Value);
        }
        else if (unionOrWardId.HasValue)
        {
            query = query.Where(x =>
                (x.UnionOrWardId.HasValue && x.UnionOrWardId == unionOrWardId.Value) ||
                (!x.UnionOrWardId.HasValue && x.Market != null && x.Market.UnionOrWardId == unionOrWardId.Value));
        }

        if (divisionId.HasValue)
        {
            query = query.Where(x =>
                (x.DivisionId.HasValue && x.DivisionId == divisionId.Value) ||
                (!x.DivisionId.HasValue && x.Market != null && x.Market.DivisionId == divisionId.Value));
        }

        if (districtId.HasValue)
        {
            query = query.Where(x =>
                (x.DistrictId.HasValue && x.DistrictId == districtId.Value) ||
                (!x.DistrictId.HasValue && x.Market != null && x.Market.DistrictId == districtId.Value));
        }

        if (upazilaId.HasValue)
        {
            query = query.Where(x =>
                (x.UpazilaId.HasValue && x.UpazilaId == upazilaId.Value) ||
                (!x.UpazilaId.HasValue && x.Market != null && x.Market.UpazilaId == upazilaId.Value));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.Product != null && x.Product.CategoryId == categoryId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.PriceDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x =>
                (x.Product != null && (
                    x.Product.NameEn.Contains(normalizedSearch) ||
                    x.Product.NameBn.Contains(normalizedSearch) ||
                    (x.Product.LocalName != null && x.Product.LocalName.Contains(normalizedSearch)))) ||
                (x.Market != null && x.Market.MarketName.Contains(normalizedSearch)));
        }

        return query;
    }

    private static IQueryable<PriceSubmission> ApplyFilters(
        IQueryable<PriceSubmission> query,
        Guid? divisionId,
        Guid? districtId,
        Guid? upazilaId,
        Guid? unionOrWardId,
        Guid? marketId,
        Guid? categoryId,
        Guid? productId,
        DateOnly? date,
        SubmissionStatus? status)
    {
        if (divisionId.HasValue)
        {
            query = query.Where(x => x.Market != null && x.Market.DivisionId == divisionId.Value);
        }

        if (districtId.HasValue)
        {
            query = query.Where(x => x.Market != null && x.Market.DistrictId == districtId.Value);
        }

        if (upazilaId.HasValue)
        {
            query = query.Where(x => x.Market != null && x.Market.UpazilaId == upazilaId.Value);
        }

        if (unionOrWardId.HasValue)
        {
            query = query.Where(x => x.Market != null && x.Market.UnionOrWardId == unionOrWardId.Value);
        }

        if (marketId.HasValue)
        {
            query = query.Where(x => x.MarketId == marketId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.Product != null && x.Product.CategoryId == categoryId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.PriceDate == date.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query;
    }
}
