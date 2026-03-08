using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.AppleMusic
{
    public class AppleMusicTrack
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("attributes")]
        public AppleMusicTrackAttributes? Attributes { get; set; }
    }

    public class AppleMusicTracksResponse
    {
        [JsonPropertyName("data")]
        public List<AppleMusicTrack>? Data { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }
}
