using CommunityToolkit.Mvvm.ComponentModel;
using Spotify.Slsk.Integration.Desktop.Services;

namespace Spotify.Slsk.Integration.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public SettingsViewModel SettingsViewModel { get; }
    public DownloadViewModel DownloadViewModel { get; }

    public MainWindowViewModel()
    {
        var settingsService = new SettingsService();
        SettingsViewModel = new SettingsViewModel(settingsService);
        DownloadViewModel = new DownloadViewModel(settingsService);
    }
}
