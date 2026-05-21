namespace BazarKoto.Contracts.Products;

public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameBn { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;
    public string? LocalName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string PrimaryUnit { get; set; } = string.Empty;
    public string ProductState { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
