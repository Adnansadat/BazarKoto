using BazarKoto.Contracts.Common;

namespace BazarKoto.Contracts.Markets;

public class MarketSearchRequest : PaginationRequest
{
    public Guid? DivisionId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? UpazilaId { get; set; }
    public Guid? UnionOrWardId { get; set; }
    public string? Search { get; set; }
    public string? MarketType { get; set; }
    public string? Status { get; set; }
}
