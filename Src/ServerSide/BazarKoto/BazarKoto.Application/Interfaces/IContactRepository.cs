using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IContactRepository
{
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ContactMessage contactMessage, CancellationToken cancellationToken = default);
}
