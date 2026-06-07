namespace BazarKoto.Contracts.Auth;

public class UpdateAdminEmailRequest
{
    public string NewEmail { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}
