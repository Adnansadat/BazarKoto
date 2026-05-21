namespace BazarKoto.Contracts.Locations;

public class LocationResponse
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? BbsCode { get; set; }
    public string? Type { get; set; }
}
