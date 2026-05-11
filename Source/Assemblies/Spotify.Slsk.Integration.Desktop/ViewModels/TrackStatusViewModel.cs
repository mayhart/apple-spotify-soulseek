using CommunityToolkit.Mvvm.ComponentModel;
using Spotify.Slsk.Integration.Models;

namespace Spotify.Slsk.Integration.Desktop.ViewModels;

public partial class TrackStatusViewModel : ObservableObject
{
    [ObservableProperty] private string _trackName = string.Empty;
    [ObservableProperty] private DownloadStatus _status = DownloadStatus.Queued;
    [ObservableProperty] private string? _failReason;

    public string StatusLabel => Status switch
    {
        DownloadStatus.Queued => "Queued",
        DownloadStatus.Downloading => "Downloading...",
        DownloadStatus.Success => "Done",
        DownloadStatus.Skipped => "Already downloaded",
        DownloadStatus.Failed => $"Failed: {FailReason}",
        _ => string.Empty
    };

    partial void OnStatusChanged(DownloadStatus value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnFailReasonChanged(string? value) => OnPropertyChanged(nameof(StatusLabel));
}
