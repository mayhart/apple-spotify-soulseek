using System.Text.Json.Serialization;

namespace Spotify.Slsk.Integration.Models.Spotify
{
    public class PageWithTracks
    {
        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("items")]
        public List<TrackItem>? Items { get; set; }
    }
}
