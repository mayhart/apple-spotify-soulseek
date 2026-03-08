using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.Spotify
{
    public class TrackItem
    {
        [JsonPropertyName("added_at")]
        public DateTime AddedAt { get; set; }

        [JsonPropertyName("track")]
        public Track? Track { get; set; }
    }
}
