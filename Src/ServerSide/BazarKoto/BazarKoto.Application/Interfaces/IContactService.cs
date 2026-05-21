using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Contact;

namespace BazarKoto.Application.Interfaces;

public interface IContactService
{
    Task<ApiResponse<ContactMessageResponse>> CreateContactMessageAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);
}
