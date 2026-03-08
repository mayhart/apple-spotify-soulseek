using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.Spotify
{
    public class Track
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist>? Artists { get; set; }

        [JsonPropertyName("album")]
        public Album? Album { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }
    }
}
