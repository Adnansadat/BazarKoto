using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
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
        return TrackablePageVisits().CountAsync(cancellationToken);
    }

    public Task<int> GetVisitCountSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return TrackablePageVisits()
            .Where(x => x.VisitedAt >= sinceUtc)
            .CountAsync(cancellationToken);
    }

    public Task<int> GetUniqueVisitorsAsync(CancellationToken cancellationToken = default)
    {
        return TrackablePageVisits()
            .Select(x => x.IpHash != string.Empty && x.UserAgent != string.Empty
                ? "ctx:" + x.IpHash + ":" + x.UserAgent
                : "vid:" + x.VisitorId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public Task<int> GetUniqueVisitorsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return TrackablePageVisits()
            .Where(x => x.VisitedAt >= sinceUtc)
            .Select(x => x.IpHash != string.Empty && x.UserAgent != string.Empty
                ? "ctx:" + x.IpHash + ":" + x.UserAgent
                : "vid:" + x.VisitorId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PageVisit>> GetRecentVisitsAsync(CancellationToken cancellationToken = default)
    {
        return await TrackablePageVisits()
            .AsNoTracking()
            .OrderByDescending(x => x.VisitedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PeakHourResponse>> GetPeakHoursAsync(CancellationToken cancellationToken = default)
    {
        return await TrackablePageVisits()
            .AsNoTracking()
            .GroupBy(x => x.VisitedAt.Hour)
            .Select(x => new PeakHourResponse
            {
                Hour = x.Key,
                VisitCount = x.Count()
            })
            .OrderByDescending(x => x.VisitCount)
            .ThenBy(x => x.Hour)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<PageVisit> TrackablePageVisits()
    {
        return _dbContext.PageVisits
            .Where(x => !x.VisitorId.StartsWith("demo-page-visitor-"))
            .Where(x => x.UserAgent != "BazarKoto Demo Browser")
            .Where(x => x.VisitorId != string.Empty || (x.IpHash != string.Empty && x.UserAgent != string.Empty));
    }
}
