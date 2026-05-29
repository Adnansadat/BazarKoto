using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class UserTrackingRepository : IUserTrackingRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public UserTrackingRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserTrackingDetails?> GetByTrackingGuidAsync(Guid trackingGuid, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserTrackingDetails.FirstOrDefaultAsync(x => x.TrackingGuid == trackingGuid, cancellationToken);
    }

    public async Task AddAsync(UserTrackingDetails userTrackingDetails, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserTrackingDetails.AddAsync(userTrackingDetails, cancellationToken);
    }

    public void Update(UserTrackingDetails userTrackingDetails)
    {
        _dbContext.UserTrackingDetails.Update(userTrackingDetails);
    }
}
