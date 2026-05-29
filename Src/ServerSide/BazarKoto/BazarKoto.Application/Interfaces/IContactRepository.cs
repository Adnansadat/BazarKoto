using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IContactRepository
{
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactMessage>> GetAsync(string? search = null, string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search = null, string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default);
    Task AddAsync(ContactMessage contactMessage, CancellationToken cancellationToken = default);
    void Update(ContactMessage contactMessage);
}
