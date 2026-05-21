using BazarKoto.Domain.Entities;

namespace BazarKoto.Infrastructure.Analytics;

public class TrafficTracker
{
    public PageVisit CreateVisit(string path, string pageTitle, string visitorId, string? referrer, string deviceType, string? country)
    {
        return new PageVisit
        {
            Path = path,
            PageTitle = pageTitle,
            VisitorId = visitorId,
            Referrer = referrer,
            DeviceType = deviceType,
            Country = country ?? string.Empty,
            VisitedAt = DateTime.UtcNow
        };
    }
}
