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
        var query = Query().AsNoTracking();

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

        return await query
            .OrderByDescending(x => x.PriceDate)
            .ThenBy(x => x.Product != null ? x.Product.NameEn : string.Empty)
            .ToListAsync(cancellationToken);
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
