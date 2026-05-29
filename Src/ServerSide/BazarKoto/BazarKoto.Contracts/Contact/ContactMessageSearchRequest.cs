using BazarKoto.Contracts.Common;

namespace BazarKoto.Contracts.Contact;

public class ContactMessageSearchRequest : PaginationRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
