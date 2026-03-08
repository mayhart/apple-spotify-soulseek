using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.Spotify
{
    public class AudioFeatures
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("key")]
        public int? Key { get; set; }

        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        [JsonPropertyName("tempo")]
        public double? Tempo { get; set; }

        [JsonPropertyName("energy")]
        public double? Energy { get; set; }

        [JsonPropertyName("danceability")]
        public double? Danceability { get; set; }

        [JsonPropertyName("valence")]
        public double? Valence { get; set; }

        [JsonPropertyName("loudness")]
        public double? Loudness { get; set; }

        [JsonPropertyName("time_signature")]
        public int? TimeSignature { get; set; }
    }
}
