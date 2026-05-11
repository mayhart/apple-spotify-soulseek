namespace Spotify.Slsk.Integration.Models
{
    public enum DownloadStatus
    {
        Queued,
        Downloading,
        Success,
        Skipped,
        Failed
    }

    public class TrackDownloadProgress
    {
        public string TrackName { get; set; } = string.Empty;
        public DownloadStatus Status { get; set; }
        public string? FailReason { get; set; }
    }
}
