using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
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

    public async Task<IReadOnlyList<ContactMessage>> GetAsync(
        string? search = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(_dbContext.ContactMessages.AsNoTracking(), search, status, dateFrom, dateTo)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search = null, string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(_dbContext.ContactMessages.AsNoTracking(), search, status, dateFrom, dateTo)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(ContactMessage contactMessage, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContactMessages.AddAsync(contactMessage, cancellationToken);
    }

    public void Update(ContactMessage contactMessage)
    {
        _dbContext.ContactMessages.Update(contactMessage);
    }

    private static IQueryable<ContactMessage> ApplyFilters(
        IQueryable<ContactMessage> query,
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalizedSearch) ||
                x.Email.Contains(normalizedSearch) ||
                x.Subject.Contains(normalizedSearch) ||
                x.Message.Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<ContactMessageStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        return query;
    }
}
