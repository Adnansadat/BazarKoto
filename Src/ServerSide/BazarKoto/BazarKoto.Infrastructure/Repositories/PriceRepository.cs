using BazarKoto.Application.Interfaces;
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
        var query = Query().AsNoTracking();

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

        return await query.OrderByDescending(x => x.PriceDate).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceSubmission>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await Query().AsNoTracking().Where(x => x.Status == SubmissionStatus.Pending).ToListAsync(cancellationToken);
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
            .Include(x => x.Product)
            .ThenInclude(x => x!.Category)
            .Include(x => x.SubmittedByUser);
    }
}
