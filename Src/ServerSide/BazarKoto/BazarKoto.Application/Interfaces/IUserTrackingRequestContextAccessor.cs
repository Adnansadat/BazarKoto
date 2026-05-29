namespace BazarKoto.Application.Interfaces;

public interface IUserTrackingRequestContextAccessor
{
    string? RawIpAddress { get; }
    string? RawUserAgent { get; }
}
