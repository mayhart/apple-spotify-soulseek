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
        /// Downloads all tracks from an Apple Music library playlist via Soulseek,
        /// using a library XML file exported from Music.app (File → Library → Export Library...).
        /// </summary>
        /// <param name="libraryXmlPath">Path to the exported Apple Music library XML file.</param>
        /// <param name="playlistId">Playlist Persistent ID from the XML. Provide either this or playlistName.</param>
        /// <param name="playlistName">Playlist name as shown in Music.app. Used when playlistId is not provided.</param>
        /// <param name="ssUsername">Soulseek username</param>
        /// <param name="ssPassword">Soulseek password</param>
        /// <param name="soulseekOptionsAction">Optional Soulseek options configurator</param>
        public async Task DownloadAppleMusicPlaylistAsync(
            string libraryXmlPath,
            string? playlistId,
            string? playlistName,
            string ssUsername,
            string ssPassword,
            Action<SoulseekOptions>? soulseekOptionsAction = null)
        {
            SoulseekOptions options = new();
            soulseekOptionsAction?.Invoke(options);

            await SoulseekService.ConnectAndLoginAsync(SoulseekClient, ssUsername, ssPassword);

            AppleMusicLibraryClient libraryClient = new(libraryXmlPath);

            AppleMusicPlaylist playlist;
            if (!string.IsNullOrEmpty(playlistId))
            {
                playlist = libraryClient.GetPlaylistById(playlistId);
            }
            else if (!string.IsNullOrEmpty(playlistName))
            {
                playlist = libraryClient.GetPlaylistByName(playlistName);
            }
            else
            {
                throw new Exception("Please provide an Apple Music playlist ID (--playlistid) or playlist name (--playlistname)");
            }

            List<TrackToDownload> tracksToDownload = playlist.Tracks.Select(track => new TrackToDownload
            {
                Query = GetQueryForAppleMusicTrack(track),
                DurationMs = track.DurationMs,
                DesiredFileName = GetDesiredFileNameForAppleMusicTrack(track)
            }).ToList();

            Log.Information($"Attempting to download '{tracksToDownload.Count}' tracks from Apple Music playlist '{playlist.Name}'...");

            SemaphoreSlim semaphoreSlim = new(5);
            IEnumerable<Task> tasks = tracksToDownload.Select(async trackToDownload =>
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    try
                    {
                        await SoulseekService.GetTrackAsync(SoulseekClient, trackToDownload, ssUsername, ssPassword, options, playlist.Name);
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
            string queryRaw = $"{trackItem.Track!.Name} {trackItem.Track.Artists![0].Name}";
            return queryRaw.RemoveSpecialCharacters();
        }

        public static string GetQueryForAppleMusicTrack(AppleMusicTrack track)
        {
            string queryRaw = $"{track.Name} {track.Artist}";
            return queryRaw.RemoveSpecialCharacters();
        }

        private static string GetDesiredFileNameForAppleMusicTrack(AppleMusicTrack track)
        {
            string raw = $"{track.Artist} - {track.Album} - {track.Name}";
            return raw.RemoveSpecialCharacters();
        }
    }
}
