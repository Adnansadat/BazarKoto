using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public AnalyticsRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddPageVisitAsync(PageVisit pageVisit, CancellationToken cancellationToken = default)
    {
        await _dbContext.PageVisits.AddAsync(pageVisit, cancellationToken);
    }

    public Task<int> GetTotalVisitsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PageVisits.CountAsync(cancellationToken);
    }

    public Task<int> GetUniqueVisitorsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PageVisits.Select(x => x.VisitorId).Distinct().CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PageVisit>> GetRecentVisitsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PageVisits.AsNoTracking().OrderByDescending(x => x.VisitedAt).Take(500).ToListAsync(cancellationToken);
    }
}
