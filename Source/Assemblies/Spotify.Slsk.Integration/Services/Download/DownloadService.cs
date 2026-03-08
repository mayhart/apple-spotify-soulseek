using Spotify.Slsk.Integration.Clients.AppleMusic;
using Spotify.Slsk.Integration.Clients.Spotify;
using Spotify.Slsk.Integration.Extensions;
using Spotify.Slsk.Integration.Models;
using Spotify.Slsk.Integration.Models.AppleMusic;
using Spotify.Slsk.Integration.Models.enums;
using Spotify.Slsk.Integration.Models.Spotify;
using Spotify.Slsk.Integration.Services.Id3Tag;
using Spotify.Slsk.Integration.Services.SoulSeek;
using Serilog;
using Soulseek;

namespace Spotify.Slsk.Integration.Services.Download
{
    public class DownloadService
    {
        private SpotifyClient SpotifyClient { get; } = new();
        private AppleMusicClient AppleMusicClient { get; } = new();
        private SoulseekClient SoulseekClient { get; }

        public DownloadService()
        {
            SoulseekClient = SoulseekService.GetClient();
        }

        // ── Spotify methods ────────────────────────────────────────────────────

        public async Task DownloadAllPlaylistTracksFromUserAsync(string spotifyUserId, string? spotifyPlaylistId, string? spotifyPlaylistName,
            string ssUsername, string ssPassword, string spotifyAccessToken, bool setId3Tags, MusicalKeyFormat musicalKeyFormat, Action<SoulseekOptions>? soulseekOptionsAction = null)
        {
            SoulseekOptions options = new();
            soulseekOptionsAction?.Invoke(options);

            await SoulseekService.ConnectAndLoginAsync(SoulseekClient, ssUsername, ssPassword);
            PlaylistItem playlistItem;
            if (spotifyPlaylistId == null)
            {
                spotifyPlaylistName = spotifyPlaylistName
                    ?? throw new Exception("Please provide playlist name or Id");
                Log.Warning($"Playlist Id not present, searching for playlist '{spotifyPlaylistName}' of user '{spotifyUserId}'...");
                playlistItem = await SpotifyClient.GetPlaylistFromUserByName(spotifyUserId, spotifyPlaylistName, spotifyAccessToken);
                spotifyPlaylistId = playlistItem.Id;
            }
            else
            {
                playlistItem = await SpotifyClient.GetPlaylistFromUser(spotifyUserId, spotifyPlaylistId, spotifyAccessToken);
            }

            List<TrackItem> trackItems = await SpotifyClient.GetAllPlaylistTracksFromUser(spotifyUserId, spotifyPlaylistId!, spotifyAccessToken);
            List<TrackToDownload> tracksToDownload = new();
            foreach (TrackItem trackItem in trackItems)
            {
                tracksToDownload.Add(new()
                {
                    Track = trackItem,
                    Query = GetQueryForSpotifyTrack(trackItem)
                });
            }

            string playlistName = playlistItem.Name!;
            await DownloadTracksInParallelAsync(ssUsername, ssPassword, spotifyAccessToken, tracksToDownload, playlistName, options, setId3Tags, musicalKeyFormat, save: false);
        }

        public async Task DownloadUnsavedPlaylistTracksFromUserAsync(string spotifyUserId, string? spotifyPlaylistId, string? spotifyPlaylistName,
            string ssUsername, string ssPassword, string spotifyAccessToken, bool setId3Tags, MusicalKeyFormat musicalKeyFormat, Action<SoulseekOptions>? soulseekOptionsAction = null)
        {
            SoulseekOptions options = new();
            soulseekOptionsAction?.Invoke(options);

            await SoulseekService.ConnectAndLoginAsync(SoulseekClient, ssUsername, ssPassword);
            PlaylistItem playlistItem = new();
            if (spotifyPlaylistId == null)
            {
                spotifyPlaylistName = spotifyPlaylistName
                    ?? throw new Exception("Please provide playlist name or Id");
                Log.Warning($"Playlist Id not present, searching for playlist '{spotifyPlaylistName}' of user '{spotifyUserId}'...");
                playlistItem = await SpotifyClient.GetPlaylistFromUserByName(spotifyUserId, spotifyPlaylistName, spotifyAccessToken);
                spotifyPlaylistId = playlistItem.Id;
            }

            List<TrackItem> trackItems = await SpotifyClient.GetUnsavedTracksInPlaylist(spotifyAccessToken, spotifyPlaylistId!);
            List<TrackToDownload> tracksToDownload = new();

            foreach (TrackItem trackItem in trackItems)
            {
                tracksToDownload.Add(new TrackToDownload()
                {
                    Track = trackItem,
                    Query = GetQueryForSpotifyTrack(trackItem)
                });
            }

            string playlistName = playlistItem.Name!;
            Log.Information($"Attempting to download '{tracksToDownload.Count}' files...");
            await DownloadTracksInParallelAsync(ssUsername, ssPassword, spotifyAccessToken, tracksToDownload, playlistName, options, setId3Tags, musicalKeyFormat, save: true);
        }

        // ── Apple Music methods ────────────────────────────────────────────────

        /// <summary>
        /// Downloads all tracks from an Apple Music library playlist via Soulseek.
        /// </summary>
        /// <param name="appleMusicDeveloperToken">Apple Music developer token (JWT signed with a MusicKit private key)</param>
        /// <param name="appleMusicUserToken">Apple Music user token (obtained via MusicKit JS or native MusicKit)</param>
        /// <param name="playlistId">Library playlist ID (e.g. p.xxxxx). Provide either this or playlistName.</param>
        /// <param name="playlistName">Library playlist name. Used when playlistId is not provided.</param>
        /// <param name="ssUsername">Soulseek username</param>
        /// <param name="ssPassword">Soulseek password</param>
        /// <param name="setId3Tags">Whether to set ID3 tags after download</param>
        /// <param name="musicalKeyFormat">Musical key format for ID3 InitialKey tag</param>
        /// <param name="soulseekOptionsAction">Optional Soulseek options configurator</param>
        public async Task DownloadAppleMusicPlaylistAsync(
            string appleMusicDeveloperToken,
            string appleMusicUserToken,
            string? playlistId,
            string? playlistName,
            string ssUsername,
            string ssPassword,
            bool setId3Tags,
            MusicalKeyFormat musicalKeyFormat,
            Action<SoulseekOptions>? soulseekOptionsAction = null)
        {
            SoulseekOptions options = new();
            soulseekOptionsAction?.Invoke(options);

            await SoulseekService.ConnectAndLoginAsync(SoulseekClient, ssUsername, ssPassword);

            if (string.IsNullOrEmpty(playlistId))
            {
                playlistName = playlistName
                    ?? throw new Exception("Please provide an Apple Music playlist ID or playlist name");
                Log.Warning($"Playlist ID not provided, searching for playlist '{playlistName}' in Apple Music library...");
                AppleMusicPlaylist found = await AppleMusicClient.GetLibraryPlaylistByNameAsync(playlistName, appleMusicDeveloperToken, appleMusicUserToken);
                playlistId = found.Id;
                playlistName = found.Attributes?.Name ?? playlistName;
            }
            else
            {
                List<AppleMusicPlaylist> playlists = await AppleMusicClient.GetAllLibraryPlaylistsAsync(appleMusicDeveloperToken, appleMusicUserToken);
                AppleMusicPlaylist? found = playlists.FirstOrDefault(p => p.Id == playlistId);
                playlistName = found?.Attributes?.Name ?? playlistId;
            }

            List<AppleMusicTrack> tracks = await AppleMusicClient.GetAllLibraryPlaylistTracksAsync(playlistId!, appleMusicDeveloperToken, appleMusicUserToken);
            List<TrackToDownload> tracksToDownload = new();

            foreach (AppleMusicTrack track in tracks)
            {
                tracksToDownload.Add(new TrackToDownload
                {
                    Query = GetQueryForAppleMusicTrack(track),
                    DurationMs = track.Attributes?.DurationInMillis,
                    DesiredFileName = GetDesiredFileNameForAppleMusicTrack(track)
                });
            }

            Log.Information($"Attempting to download '{tracksToDownload.Count}' Apple Music tracks from playlist '{playlistName}'...");

            SemaphoreSlim semaphoreSlim = new(5);
            IEnumerable<Task> tasks = tracksToDownload.Select(async trackToDownload =>
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    SoulseekResult result = new();
                    try
                    {
                        result = await SoulseekService.GetTrackAsync(SoulseekClient, trackToDownload, ssUsername, ssPassword, options, playlistName);
                        Log.Information($"Downloads remaining: '{tracksToDownload.Count - (tracksToDownload.IndexOf(trackToDownload) + 1)}'");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Something went wrong downloading '{trackToDownload.Query}': {e.Message}");
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        // ── Shared helpers ─────────────────────────────────────────────────────

        private async Task DownloadTracksInParallelAsync(string ssUsername, string ssPassword, string spotifyAccessToken, List<TrackToDownload> tracksToDownload, string playlistName,
            SoulseekOptions options, bool setId3Tags, MusicalKeyFormat musicalKeyFormat, bool save = true)
        {
            SemaphoreSlim semaphoreSlim = new(5);
            IEnumerable<Task> tasks = tracksToDownload.Select(async trackToDownload =>
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    await DownloadSpotifyTrackAsync(ssUsername, ssPassword, spotifyAccessToken, playlistName, trackToDownload, options, setId3Tags, musicalKeyFormat, save);
                    Log.Information($"Downloads remaining: '{tracksToDownload.Count - (tracksToDownload.IndexOf(trackToDownload) + 1)}'");
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<bool> DownloadSpotifyTrackAsync(string ssUsername, string ssPassword, string spotifyAccessToken, string playlistName,
            TrackToDownload trackToDownload, SoulseekOptions soulseekOptions, bool setId3Tags, MusicalKeyFormat musicalKeyFormat, bool save = false)
        {
            SoulseekResult result = new();
            try
            {
                result = await SoulseekService.GetTrackAsync(SoulseekClient, trackToDownload, ssUsername, ssPassword, soulseekOptions, playlistName);
            }
            catch (Exception e)
            {
                Log.Error($"Something went wrong downloading '{trackToDownload.Query}', stacktrace:");
                Log.Error($"{e.StackTrace}");
            }

            if (result.Success && save)
            {
                Log.Information($"Download successful, saving track in spotify...");
                await SpotifyClient.SaveTrackAsync(spotifyAccessToken, trackToDownload.Track!);
            }

            if (result.Success && setId3Tags)
            {
                Log.Information($"Setting ID3 tags of downloaded file...");
                AudioFeatures audioFeatures = await SpotifyClient.GetTrackAudioFeatures(trackToDownload.Track!.Track!.Id!, spotifyAccessToken);
                Id3TagService.SetId3Tags(trackToDownload.Track, audioFeatures, musicalKeyFormat, result.FilePath!);
            }

            return result.Success;
        }

        public static string GetQueryForSpotifyTrack(TrackItem trackItem)
        {
            string queryRaw = $"{trackItem.Track!.Name} {trackItem.Track.Artists![0].Name} {trackItem.Track.Album!.Name}";
            return queryRaw.RemoveSpecialCharacters();
        }

        public static string GetQueryForAppleMusicTrack(AppleMusicTrack track)
        {
            string queryRaw = $"{track.Attributes?.Name} {track.Attributes?.ArtistName} {track.Attributes?.AlbumName}";
            return queryRaw.RemoveSpecialCharacters();
        }

        private static string GetDesiredFileNameForAppleMusicTrack(AppleMusicTrack track)
        {
            string raw = $"{track.Attributes?.ArtistName} - {track.Attributes?.AlbumName} - {track.Attributes?.Name}";
            return raw.RemoveSpecialCharacters();
        }
    }
}
