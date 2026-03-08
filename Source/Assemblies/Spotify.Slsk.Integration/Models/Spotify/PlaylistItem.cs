using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.Spotify
{
    public class PlaylistItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
