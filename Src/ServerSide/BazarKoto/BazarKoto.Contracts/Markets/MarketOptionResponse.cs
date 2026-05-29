namespace BazarKoto.Contracts.Markets;

public class MarketOptionResponse
{
    public Guid Id { get; set; }
    public Guid MarketId { get; set; }
    public string MarketName { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
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
}
