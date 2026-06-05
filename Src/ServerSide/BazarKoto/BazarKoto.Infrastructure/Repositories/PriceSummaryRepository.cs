using BazarKoto.Application.Features.Prices;
using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class PriceSummaryRepository : IPriceSummaryRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public PriceSummaryRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DailyPriceSummary>> GetAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
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
            date);

        return await ApplyOrdering(query).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<DailyPriceSummary> Items, int TotalCount)> GetPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            _dbContext.DailyPriceSummaries.AsNoTracking(),
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageIds = await ApplyOrdering(query)
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

    public Task<PriceSummaryAggregate?> GetAggregateAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            _dbContext.DailyPriceSummaries.AsNoTracking(),
            divisionId,
            districtId,
            upazilaId,
            unionOrWardId,
            marketId,
            categoryId,
            productId,
            date);

        return query
            .GroupBy(_ => 1)
            .Select(group => new PriceSummaryAggregate
            {
                ProductId = group
                    .OrderByDescending(x => x.PriceDate)
                    .ThenBy(x => x.Product != null ? x.Product.NameEn : string.Empty)
                    .Select(x => x.ProductId)
                    .FirstOrDefault(),
                ProductName = group
                    .OrderByDescending(x => x.PriceDate)
                    .ThenBy(x => x.Product != null ? x.Product.NameEn : string.Empty)
                    .Select(x => x.Product != null ? x.Product.NameEn : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                Unit = group
                    .OrderByDescending(x => x.PriceDate)
                    .ThenBy(x => x.Product != null ? x.Product.NameEn : string.Empty)
                    .Select(x => x.Product != null ? x.Product.PrimaryUnit : string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                MinimumPrice = group.Min(x => x.MinPrice),
                MaximumPrice = group.Max(x => x.MaxPrice),
                WeightedPriceTotal = group.Sum(x => x.AveragePrice * x.SubmissionCount),
                SubmissionCount = group.Sum(x => x.SubmissionCount),
                FromDate = group.Min(x => x.PriceDate),
                ToDate = group.Max(x => x.PriceDate)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IOrderedQueryable<DailyPriceSummary> ApplyOrdering(IQueryable<DailyPriceSummary> query)
    {
        return query
            .OrderByDescending(x => x.PriceDate)
            .ThenBy(x => x.Product != null ? x.Product.NameEn : string.Empty);
    }

    private static IQueryable<DailyPriceSummary> ApplyFilters(
        IQueryable<DailyPriceSummary> query,
        Guid? divisionId,
        Guid? districtId,
        Guid? upazilaId,
        Guid? unionOrWardId,
        Guid? marketId,
        Guid? categoryId,
        Guid? productId,
        DateOnly? date)
    {
        if (divisionId.HasValue)
        {
            query = query.Where(x => x.DivisionId == divisionId.Value);
        }

        if (districtId.HasValue)
        {
            query = query.Where(x => x.DistrictId == districtId.Value);
        }

        if (upazilaId.HasValue)
        {
            query = query.Where(x => x.UpazilaId == upazilaId.Value);
        }

        if (unionOrWardId.HasValue)
        {
            query = query.Where(x => x.UnionOrWardId == unionOrWardId.Value);
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

        return query;
    }

    public Task<DailyPriceSummary?> GetForUpsertAsync(
        Guid productId,
        Guid? marketId,
        Guid divisionId,
        Guid districtId,
        Guid upazilaId,
        Guid? unionOrWardId,
        DateOnly priceDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DailyPriceSummaries.FirstOrDefaultAsync(
            x => x.ProductId == productId
                && x.MarketId == marketId
                && x.DivisionId == divisionId
                && x.DistrictId == districtId
                && x.UpazilaId == upazilaId
                && x.UnionOrWardId == unionOrWardId
                && x.PriceDate == priceDate,
            cancellationToken);
    }

    public async Task AddAsync(DailyPriceSummary summary, CancellationToken cancellationToken = default)
    {
        await _dbContext.DailyPriceSummaries.AddAsync(summary, cancellationToken);
    }

    private IQueryable<DailyPriceSummary> Query()
    {
        return _dbContext.DailyPriceSummaries
            .Include(x => x.Product)
            .ThenInclude(x => x!.Category)
            .Include(x => x.Market);
    }
}
