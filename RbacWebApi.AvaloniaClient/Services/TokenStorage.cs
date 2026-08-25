using System.Text.Json;

namespace RbacWebApi.AvaloniaClient.Services;

/// <summary>简单的本地Token持久化（保存到用户临时目录）</summary>
public static class TokenStorage
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RbacWebApiClient",
        "token.json");

    public record StoredToken(string Token, string UserId, string Username, DateTime ExpiresAt);

    public static void Save(StoredToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(token);
        File.WriteAllText(FilePath, json);
    }

    public static StoredToken? Load()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<StoredToken>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
