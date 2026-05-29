namespace BazarKoto.Contracts.Products;

public class ProductOptionResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameBn { get; set; } = string.Empty;
    public string? LocalOrAlternateName { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameBn { get; set; } = string.Empty;
    public string PrimaryUnit { get; set; } = string.Empty;
    public string ProductState { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
}
