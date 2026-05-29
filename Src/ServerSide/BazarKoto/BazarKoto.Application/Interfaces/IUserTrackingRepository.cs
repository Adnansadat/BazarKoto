using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IUserTrackingRepository
{
    Task<UserTrackingDetails?> GetByTrackingGuidAsync(Guid trackingGuid, CancellationToken cancellationToken = default);
    Task AddAsync(UserTrackingDetails userTrackingDetails, CancellationToken cancellationToken = default);
    void Update(UserTrackingDetails userTrackingDetails);
}
