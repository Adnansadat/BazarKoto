using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public ContactRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(ContactMessage contactMessage, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContactMessages.AddAsync(contactMessage, cancellationToken);
    }
}
