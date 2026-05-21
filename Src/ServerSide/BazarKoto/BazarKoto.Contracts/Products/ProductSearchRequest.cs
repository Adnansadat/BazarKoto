using BazarKoto.Contracts.Common;

namespace BazarKoto.Contracts.Products;

public class ProductSearchRequest : PaginationRequest
{
    public Guid? CategoryId { get; set; }
    public string? Search { get; set; }
}
