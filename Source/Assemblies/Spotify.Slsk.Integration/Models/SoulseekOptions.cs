namespace Spotify.Slsk.Integration.Models
{
    public class SoulseekOptions
    {
        public bool AllowFlac { get; set; }
        public bool SkipResults { get; set; }
        public int SearchTimeout { get; set; } = 10;
    }
}
