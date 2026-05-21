namespace BazarKoto.Contracts.Admin;

public class ModerationQueueResponse
{
    public int PendingMarkets { get; set; }
    public int PendingProducts { get; set; }
    public int PendingPriceSubmissions { get; set; }
    public int FlaggedPriceSubmissions { get; set; }
    public int PendingContactMessages { get; set; }
}
