using System.Threading.Tasks;
using Spotify.Slsk.Integration.Cli.Commands.SubCommands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Spotify.Slsk.Integration.Cli.Commands
{
    [Command(Name = "spotseek", Description = "Download tracks from Spotify or Apple Music playlists via Soulseek")]
    [Subcommand(
        typeof(DownloadPlaylistCommand),
        typeof(DownloadAndSavePlaylistCommand),
        typeof(DownloadTrackCommand),
        typeof(TranslateMusicalKeyCommand),
        typeof(DownloadAppleMusicPlaylistCommand))]
    class SpotseekCommand : SpotSeekCommandBase
    {
        public SpotseekCommand(ILogger<SpotseekCommand> logger, IConsole console)
        {
            _logger = logger;
            _console = console;
        }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}
