namespace Spotify.Slsk.Integration.Models.AppleMusic
{
    public class AppleMusicTrack
    {
        /// <summary>Track ID from the library XML.</summary>
        public string? Id { get; set; }

        public string? Name { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }

        /// <summary>Duration in milliseconds (Total Time field from the XML).</summary>
        public int? DurationMs { get; set; }
    }
}
