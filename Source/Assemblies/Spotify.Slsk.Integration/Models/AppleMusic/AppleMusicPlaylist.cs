namespace Spotify.Slsk.Integration.Models.AppleMusic
{
    public class AppleMusicPlaylist
    {
        /// <summary>Playlist Persistent ID from the library XML.</summary>
        public string? Id { get; set; }

        /// <summary>Playlist name.</summary>
        public string? Name { get; set; }

        /// <summary>Tracks belonging to this playlist.</summary>
        public List<AppleMusicTrack> Tracks { get; set; } = new();
    }
}
