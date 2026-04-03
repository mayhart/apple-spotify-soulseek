namespace Spotify.Slsk.Integration.Desktop.Models;

public class AppSettings
{
    public string SoulseekUsername { get; set; } = string.Empty;
    public string SoulseekPasswordEncrypted { get; set; } = string.Empty;
    public string LastSpotifyUserId { get; set; } = string.Empty;
    public string LastOutputFolder { get; set; } = string.Empty;
    public bool SetId3Tags { get; set; } = true;
    public bool AllowFlac { get; set; } = false;
    public bool SkipExistingResults { get; set; } = true;
    public int SearchTimeout { get; set; } = 10;
    public string KeyFormat { get; set; } = "open-key";
}
