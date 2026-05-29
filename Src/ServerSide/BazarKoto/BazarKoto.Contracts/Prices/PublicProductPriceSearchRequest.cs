using BazarKoto.Contracts.Common;

namespace BazarKoto.Contracts.Prices;

public class PublicProductPriceSearchRequest : PaginationRequest
{
    public Guid? DivisionId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? UpazilaId { get; set; }
    public Guid? UnionOrWardId { get; set; }
    public Guid? MarketId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ProductId { get; set; }
    public DateOnly? Date { get; set; }
    public string? Search { get; set; }
}
