using System.IO;

namespace Spotify_DropDown.Services;

public static class TokenStorage
{
    private static readonly string FilePath = "spotify_refresh_token.txt";

    public static void SaveRefreshToken(string token)
    {
        File.WriteAllText(FilePath, token);
    }

    public static string? LoadRefreshToken()
    {
        if (!File.Exists(FilePath)) return null;

        return File.ReadAllText(FilePath);
    }
}
