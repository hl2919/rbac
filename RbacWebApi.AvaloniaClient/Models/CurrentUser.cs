namespace RbacWebApi.AvaloniaClient.Models;

public class CurrentUser
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public List<string> Roles { get; set; } = [];
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && ExpiresAt > DateTime.Now;
}
