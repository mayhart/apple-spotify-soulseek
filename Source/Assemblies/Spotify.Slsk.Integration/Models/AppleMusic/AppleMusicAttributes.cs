using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.AppleMusic
{
    public class AppleMusicPlaylistAttributes
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public AppleMusicEditorialNotes? Description { get; set; }
    }

    public class AppleMusicEditorialNotes
    {
        [JsonPropertyName("standard")]
        public string? Standard { get; set; }
    }

    public class AppleMusicTrackAttributes
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("artistName")]
        public string? ArtistName { get; set; }

        [JsonPropertyName("albumName")]
        public string? AlbumName { get; set; }

        [JsonPropertyName("durationInMillis")]
        public int? DurationInMillis { get; set; }
    }
}
