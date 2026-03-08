using Spotify.Slsk.Integration.Models.AppleMusic;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static Spotify.Slsk.Integration.Constants.Constants;

namespace Spotify.Slsk.Integration.Clients.AppleMusic
{
    public class AppleMusicClient
    {
        private HttpClient HttpClient { get; } = new();

        /// <summary>
        /// Gets all playlists from the user's Apple Music library.
        /// </summary>
        /// <param name="developerToken">Apple Music developer token (JWT)</param>
        /// <param name="userToken">Apple Music user token obtained via MusicKit</param>
        public async Task<List<AppleMusicPlaylist>> GetAllLibraryPlaylistsAsync(string developerToken, string userToken)
        {
            List<AppleMusicPlaylist> result = new();
            string? nextUrl = $"{APPLE_MUSIC_BASE_URL}me/library/playlists?limit=100";

            while (nextUrl != null)
            {
                HttpRequestMessage request = new(HttpMethod.Get, nextUrl);
                HttpResponseMessage response = await GetResponseAsync(request, developerToken, userToken);

                AppleMusicPlaylistsResponse page = await DeserializeResponseAsync<AppleMusicPlaylistsResponse>(response);
                if (page.Data != null)
                {
                    result.AddRange(page.Data);
                }

                nextUrl = page.Next != null ? $"https://api.music.apple.com{page.Next}" : null;
            }

            return result;
        }

        /// <summary>
        /// Gets a library playlist by name.
        /// </summary>
        public async Task<AppleMusicPlaylist> GetLibraryPlaylistByNameAsync(string playlistName, string developerToken, string userToken)
        {
            List<AppleMusicPlaylist> playlists = await GetAllLibraryPlaylistsAsync(developerToken, userToken);
            AppleMusicPlaylist? playlist = playlists.FirstOrDefault(p => p.Attributes?.Name == playlistName);
            return playlist
                ?? throw new Exception($"Apple Music playlist with name '{playlistName}' not found in library");
        }

        /// <summary>
        /// Gets all tracks from a library playlist.
        /// </summary>
        /// <param name="playlistId">Apple Music library playlist ID (e.g. p.xxxxx)</param>
        /// <param name="developerToken">Apple Music developer token (JWT)</param>
        /// <param name="userToken">Apple Music user token obtained via MusicKit</param>
        public async Task<List<AppleMusicTrack>> GetAllLibraryPlaylistTracksAsync(string playlistId, string developerToken, string userToken)
        {
            List<AppleMusicTrack> result = new();
            string? nextUrl = $"{APPLE_MUSIC_BASE_URL}me/library/playlists/{playlistId}/tracks?limit=100";

            while (nextUrl != null)
            {
                HttpRequestMessage request = new(HttpMethod.Get, nextUrl);
                HttpResponseMessage response = await GetResponseAsync(request, developerToken, userToken);

                AppleMusicTracksResponse page = await DeserializeResponseAsync<AppleMusicTracksResponse>(response);
                if (page.Data != null)
                {
                    result.AddRange(page.Data);
                }

                nextUrl = page.Next != null ? $"https://api.music.apple.com{page.Next}" : null;
            }

            return result;
        }

        private async Task<HttpResponseMessage> GetResponseAsync(HttpRequestMessage request, string developerToken, string userToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", developerToken);
            request.Headers.Add("Music-User-Token", userToken);

            HttpResponseMessage response = await HttpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Unauthorized: please verify your Apple Music developer token and user token are valid");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception("Forbidden: the developer token may be invalid or expired");
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Apple Music API error ({response.StatusCode}): {body}");
            }

            return response;
        }

        private static async Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage httpResponseMessage)
        {
            string responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }
    }
}
