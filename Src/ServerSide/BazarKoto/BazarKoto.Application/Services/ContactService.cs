using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContactService(IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ContactMessageResponse>> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default)
    {
        var contactMessage = new ContactMessage
        {
            Name = request.Name,
            Email = request.Email,
            Subject = request.Subject,
            Message = request.Message,
            Status = ContactMessageStatus.New
        };

        await _contactRepository.AddAsync(contactMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ContactMessageResponse>.Ok(new ContactMessageResponse
        {
            Id = contactMessage.Id,
            Name = contactMessage.Name,
            Email = contactMessage.Email,
            Subject = contactMessage.Subject,
            Message = contactMessage.Message,
            Status = contactMessage.Status.ToString(),
            ResolvedAt = contactMessage.ResolvedAt,
            CreatedAt = contactMessage.CreatedAt,
            UpdatedAt = contactMessage.UpdatedAt
        }, "Message sent successfully.");
    }
}
