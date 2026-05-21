namespace BazarKoto.Contracts.Markets;

public class MarketResponse
{
    public Guid Id { get; set; }
    public Guid DivisionId { get; set; }
    public string DivisionNameEn { get; set; } = string.Empty;
    public string DivisionNameBn { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public string DistrictNameEn { get; set; } = string.Empty;
    public string DistrictNameBn { get; set; } = string.Empty;
    public Guid UpazilaId { get; set; }
    public string UpazilaNameEn { get; set; } = string.Empty;
    public string UpazilaNameBn { get; set; } = string.Empty;
    public Guid? UnionOrWardId { get; set; }
    public string? UnionOrWardNameEn { get; set; }
    public string? UnionOrWardNameBn { get; set; }
    public string Area { get; set; } = string.Empty;
    public string MarketName { get; set; } = string.Empty;
    public string VillageOrMoholla { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string MarketType { get; set; } = string.Empty;
    public string OperatingSchedule { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
