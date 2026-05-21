namespace BazarKoto.Contracts.Markets;

public class UpdateMarketRequest
{
    public Guid DivisionId { get; set; }
    public Guid DistrictId { get; set; }
    public Guid UpazilaId { get; set; }
    public Guid? UnionOrWardId { get; set; }
    public string Area { get; set; } = string.Empty;
    public string MarketName { get; set; } = string.Empty;
    public string VillageOrMoholla { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string MarketType { get; set; } = string.Empty;
    public string OperatingSchedule { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
