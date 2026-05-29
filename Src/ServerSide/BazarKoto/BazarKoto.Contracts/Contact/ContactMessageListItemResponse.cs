namespace BazarKoto.Contracts.Contact;

public class ContactMessageListItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasScreenshot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
