using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;

namespace BazarKoto.Application.Interfaces;

public interface IContactService
{
    Task<ApiResponse<ContactMessageResponse>> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ContactMessageListItemResponse>> GetContactMessagesAsync(ContactMessageSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ContactMessageResponse>> GetContactMessageAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ContactMessageResponse>> UpdateContactMessageStatusAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ContactMessageResponse>> UpdateContactMessageNoteAsync(Guid id, UpdateContactMessageNoteRequest request, CancellationToken cancellationToken = default);
}
