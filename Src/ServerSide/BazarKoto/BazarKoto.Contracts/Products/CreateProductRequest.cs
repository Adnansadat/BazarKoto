namespace BazarKoto.Contracts.Products;

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;
    public string? LocalName { get; set; }
    public string? Slug { get; set; }
    public string PrimaryUnit { get; set; } = string.Empty;
    public string ProductState { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
