using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IUserTrackingService
{
    Task<UserTrackingResult> CreateOrUpdateAsync(UserTrackingInput? input = null, CancellationToken cancellationToken = default);
    Task<UserTrackingDetails> CreateOrUpdateEntityAsync(UserTrackingInput? input = null, CancellationToken cancellationToken = default);
}
