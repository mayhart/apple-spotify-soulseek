using Spotify.Slsk.Integration.Models.Spotify;
using Soulseek;
using File = Soulseek.File;

namespace Spotify.Slsk.Integration.Models
{
    public class TrackToDownload
    {
        /// <summary>
        /// Spotify track item. May be null when downloading Apple Music or plain query tracks.
        /// </summary>
        public TrackItem? Track { get; set; }

        /// <summary>
        /// Soulseek search query.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Track duration in milliseconds used to match Soulseek results.
        /// Overrides the duration from the Spotify Track when set explicitly.
        /// </summary>
        public int? DurationMs { get; set; }

        /// <summary>
        /// Desired output file name (without extension). Overrides the auto-generated name.
        /// </summary>
        public string? DesiredFileName { get; set; }

        public List<SearchResponse>? SearchResponses { get; set; }
        public SearchResponse? SelectedSearchResponse { get; set; }
        public List<File?>? Files { get; set; }
        public File? SelectedFile { get; set; }
        public string? Username { get; set; }

        /// <summary>
        /// Returns the effective duration in milliseconds, preferring explicitly set DurationMs.
        /// </summary>
        public int? EffectiveDurationMs => DurationMs ?? Track?.Track?.DurationMs;
    }
}
