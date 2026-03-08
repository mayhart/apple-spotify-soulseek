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
    [Command(Name = "download-apple-playlist", Description = "Downloads all tracks from an Apple Music library playlist via Soulseek")]
    class DownloadAppleMusicPlaylistCommand : SpotSeekCommandBase
    {
        [Option(CommandOptionType.SingleValue, ShortName = "d", LongName = "developertoken", Description = "Apple Music developer token (JWT)", ValueName = "developer token", ShowInHelpText = true)]
        public string AppleMusicDeveloperToken { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "m", LongName = "usertoken", Description = "Apple Music user token (from MusicKit)", ValueName = "user token", ShowInHelpText = true)]
        public string AppleMusicUserToken { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "l", LongName = "playlistid", Description = "Apple Music library playlist ID (e.g. p.xxxxx)", ValueName = "playlist id", ShowInHelpText = true)]
        public string PlaylistId { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "n", LongName = "playlistname", Description = "Apple Music library playlist name", ValueName = "playlist name", ShowInHelpText = true)]
        public string PlaylistName { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "u", LongName = "ssusername", Description = "Soulseek login username", ValueName = "login username", ShowInHelpText = true)]
        public string SSUsername { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "p", LongName = "sspassword", Description = "Soulseek login password", ValueName = "login password", ShowInHelpText = true)]
        public string SSPassword { get; set; }

        [Option(CommandOptionType.NoValue, ShortName = "g", LongName = "id3tags", Description = "Set ID3 tags after download", ValueName = "set id3 tags", ShowInHelpText = true)]
        public bool SetId3Tags { get; set; }

        [Option(CommandOptionType.SingleValue, ShortName = "k", LongName = "keyformat", Description = "Desired format for id3tag 'InitialKey'", ValueName = "key format", ShowInHelpText = true)]
        public string DesiredKeyFormat { get; set; } = MusicalKeyFormat.OpenKey.Value;

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
            if (string.IsNullOrEmpty(AppleMusicDeveloperToken))
            {
                AppleMusicDeveloperToken = Prompt.GetString("Apple Music developer token:", AppleMusicDeveloperToken);
            }

            if (string.IsNullOrEmpty(AppleMusicUserToken))
            {
                AppleMusicUserToken = Prompt.GetString("Apple Music user token:", AppleMusicUserToken);
            }

            if (string.IsNullOrEmpty(PlaylistId) && string.IsNullOrEmpty(PlaylistName))
            {
                PlaylistId = Prompt.GetString("Apple Music playlist ID (leave blank to use name):", PlaylistId);
            }

            if (string.IsNullOrEmpty(PlaylistId) && string.IsNullOrEmpty(PlaylistName))
            {
                PlaylistName = Prompt.GetString("Apple Music playlist name:", PlaylistName);
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

                Log.Information($"SetId3Tags is set to '{SetId3Tags}'");
                Log.Information($"AllowFlac is set to '{AllowFlac}'");
                Log.Information($"SkipPresentResults is set to '{SkipPresentResults}'");

                await _downloadService.DownloadAppleMusicPlaylistAsync(
                    AppleMusicDeveloperToken,
                    AppleMusicUserToken,
                    PlaylistId,
                    PlaylistName,
                    SSUsername,
                    SSPassword,
                    SetId3Tags,
                    MusicalKeyFormat.from(DesiredKeyFormat),
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
