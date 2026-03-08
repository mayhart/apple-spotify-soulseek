using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.AppleMusic
{
    public class AppleMusicPlaylist
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("attributes")]
        public AppleMusicPlaylistAttributes? Attributes { get; set; }
    }

    public class AppleMusicPlaylistsResponse
    {
        [JsonPropertyName("data")]
        public List<AppleMusicPlaylist>? Data { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }
}
