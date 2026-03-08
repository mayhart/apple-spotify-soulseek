using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Spotify.Slsk.Integration.Models;
using Spotify.Slsk.Integration.Models.enums;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Spotify.Slsk.Integration.Cli.Commands.SubCommands
{
    [Command(Name = "download-apple-playlist", Description = "Downloads all tracks from an Apple Music library playlist via Soulseek, using an exported library XML file")]
    class DownloadAppleMusicPlaylistCommand : SpotSeekCommandBase
    {
        [Option(CommandOptionType.SingleValue, ShortName = "x", LongName = "library", Description = "Path to the Apple Music library XML file (export via Music.app → File → Library → Export Library...)", ValueName = "library xml path", ShowInHelpText = true)]
        public string LibraryXmlPath { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "l", LongName = "playlistid", Description = "Playlist Persistent ID from the XML", ValueName = "playlist id", ShowInHelpText = true)]
        public string PlaylistId { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "n", LongName = "playlistname", Description = "Playlist name exactly as shown in Music.app", ValueName = "playlist name", ShowInHelpText = true)]
        public string PlaylistName { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "u", LongName = "ssusername", Description = "Soulseek login username", ValueName = "login username", ShowInHelpText = true)]
        public string SSUsername { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "p", LongName = "sspassword", Description = "Soulseek login password", ValueName = "login password", ShowInHelpText = true)]
        public string SSPassword { get; set; }

        [Option(CommandOptionType.NoValue, ShortName = "s", LongName = "skip-results", Description = "Skip tracks already present in results folder", ValueName = "skip present results", ShowInHelpText = true)]
        public bool SkipPresentResults { get; }

        [Option(CommandOptionType.NoValue, ShortName = "f", LongName = "flac", Description = "Allow FLAC files", ValueName = "allow flac", ShowInHelpText = true)]
        public bool AllowFlac { get; }

        public const int DEFAULT_SEARCH_TIMEOUT = 10;
        [Option(CommandOptionType.SingleValue, ShortName = "t", LongName = "searchtimeout", Description = "Max searching time per query (seconds)", ValueName = "search timeout", ShowInHelpText = true)]
        public int? SearchTimeout { get; set; }

        public DownloadAppleMusicPlaylistCommand(ILogger<DownloadAppleMusicPlaylistCommand> logger, IConsole console)
        {
            _logger = logger;
            _console = console;
        }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(LibraryXmlPath))
            {
                LibraryXmlPath = Prompt.GetString("Path to Apple Music library XML file:", LibraryXmlPath);
            }

            if (string.IsNullOrEmpty(PlaylistId) && string.IsNullOrEmpty(PlaylistName))
            {
                PlaylistId = Prompt.GetString("Playlist Persistent ID (leave blank to use name):", PlaylistId);
            }

            if (string.IsNullOrEmpty(PlaylistId) && string.IsNullOrEmpty(PlaylistName))
            {
                PlaylistName = Prompt.GetString("Playlist name:", PlaylistName);
            }

            if (string.IsNullOrEmpty(SSUsername))
            {
                SSUsername = Prompt.GetString("Soulseek user name:", SSUsername);
            }

            if (string.IsNullOrEmpty(SSPassword))
            {
                SSPassword = SecureStringToString(Prompt.GetPasswordAsSecureString("Soulseek password:"));
            }

            SearchTimeout ??= Prompt.GetInt("Max time per search query?", DEFAULT_SEARCH_TIMEOUT);

            try
            {
                await base.OnExecute(app);

                UserProfile userProfile = new()
                {
                    Username = SSUsername,
                    Password = Encrypt(SSPassword),
                };

                if (!Directory.Exists(ProfileFolder))
                {
                    Directory.CreateDirectory(ProfileFolder);
                }

                await File.WriteAllTextAsync($"{ProfileFolder}{Profile}", JsonSerializer.Serialize(userProfile), Encoding.UTF8);

                Log.Information($"AllowFlac is set to '{AllowFlac}'");
                Log.Information($"SkipPresentResults is set to '{SkipPresentResults}'");

                await _downloadService.DownloadAppleMusicPlaylistAsync(
                    LibraryXmlPath,
                    PlaylistId,
                    PlaylistName,
                    SSUsername,
                    SSPassword,
                    options =>
                    {
                        options.AllowFlac = AllowFlac;
                        options.SkipResults = SkipPresentResults;
                        options.SearchTimeout = SearchTimeout.Value;
                    });

                return 0;
            }
            catch (Exception ex)
            {
                OnException(ex);
                return 1;
            }
        }
    }
}
