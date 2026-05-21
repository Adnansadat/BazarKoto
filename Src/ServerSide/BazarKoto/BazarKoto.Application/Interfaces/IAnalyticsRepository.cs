using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IAnalyticsRepository
{
    Task AddPageVisitAsync(PageVisit pageVisit, CancellationToken cancellationToken = default);
    Task<int> GetTotalVisitsAsync(CancellationToken cancellationToken = default);
    Task<int> GetUniqueVisitorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PageVisit>> GetRecentVisitsAsync(CancellationToken cancellationToken = default);
}
