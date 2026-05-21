namespace BazarKoto.Application.Interfaces;

public interface IAdminAuditService
{
    Task LogAsync(string action, string entityName, string? entityId = null, string? oldValueJson = null, string? newValueJson = null, CancellationToken cancellationToken = default);
}
