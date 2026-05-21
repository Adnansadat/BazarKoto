using BazarKoto.Contracts.Analytics;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Infrastructure.Analytics;

public class PeakHourCalculator
{
    public IReadOnlyList<PeakHourResponse> Calculate(IEnumerable<PageVisit> visits)
    {
        return visits
            .GroupBy(x => x.VisitedAt.Hour)
            .Select(x => new PeakHourResponse { Hour = x.Key, VisitCount = x.Count() })
            .OrderByDescending(x => x.VisitCount)
            .ThenBy(x => x.Hour)
            .ToList();
    }
}
