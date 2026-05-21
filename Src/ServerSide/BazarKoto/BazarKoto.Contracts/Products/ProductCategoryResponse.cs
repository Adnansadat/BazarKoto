namespace BazarKoto.Contracts.Products;

public class ProductCategoryResponse
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameBn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionBn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
