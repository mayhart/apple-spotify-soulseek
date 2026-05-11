using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spotify.Slsk.Integration.Desktop.Models;
using Spotify.Slsk.Integration.Desktop.Services;
using Spotify.Slsk.Integration.Models;
using Spotify.Slsk.Integration.Models.enums;
using Spotify.Slsk.Integration.Services.Download;

namespace Spotify.Slsk.Integration.Desktop.ViewModels;

public enum DownloadSource { SpotifyPlaylist, SpotifyTrack, AppleMusicPlaylist }

public partial class DownloadViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DownloadService _downloadService;
    private CancellationTokenSource? _cts;

    // ── Source ────────────────────────────────────────────────────────────────

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsSpotify))]
    [NotifyPropertyChangedFor(nameof(IsAppleMusic))]
    [NotifyPropertyChangedFor(nameof(ShowPlaylistFields))]
    [NotifyPropertyChangedFor(nameof(ShowTrackField))]
    [NotifyPropertyChangedFor(nameof(IsSpotifyPlaylistSelected))]
    [NotifyPropertyChangedFor(nameof(IsAppleMusicPlaylistSelected))]
    private DownloadSource _selectedSource = DownloadSource.SpotifyPlaylist;

    public bool IsSpotify => SelectedSource != DownloadSource.AppleMusicPlaylist;
    public bool IsAppleMusic => SelectedSource == DownloadSource.AppleMusicPlaylist;
    public bool ShowPlaylistFields => SelectedSource != DownloadSource.SpotifyTrack;
    public bool ShowTrackField => SelectedSource == DownloadSource.SpotifyTrack;

    public bool IsSpotifyPlaylistSelected
    {
        get => SelectedSource == DownloadSource.SpotifyPlaylist;
        set { if (value) SelectedSource = DownloadSource.SpotifyPlaylist; }
    }

    public bool IsAppleMusicPlaylistSelected
    {
        get => SelectedSource == DownloadSource.AppleMusicPlaylist;
        set { if (value) SelectedSource = DownloadSource.AppleMusicPlaylist; }
    }

    // ── Spotify inputs ────────────────────────────────────────────────────────

    [ObservableProperty] private string _spotifyUserId = string.Empty;
    [ObservableProperty] private string _spotifyPlaylistId = string.Empty;
    [ObservableProperty] private string _spotifyPlaylistName = string.Empty;
    [ObservableProperty] private string _spotifyTrackId = string.Empty;
    [ObservableProperty] private string _spotifyAccessToken = string.Empty;

    // ── Apple Music inputs ────────────────────────────────────────────────────

    [ObservableProperty] private string _appleMusicXmlPath = string.Empty;
    [ObservableProperty] private string _appleMusicPlaylistId = string.Empty;
    [ObservableProperty] private string _appleMusicPlaylistName = string.Empty;

    // ── Soulseek credentials (from settings) ──────────────────────────────────

    [ObservableProperty] private string _soulseekUsername = string.Empty;
    [ObservableProperty] private string _soulseekPassword = string.Empty;

    // ── Options ───────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _setId3Tags = true;
    [ObservableProperty] private bool _allowFlac = false;
    [ObservableProperty] private bool _skipExistingResults = true;
    [ObservableProperty] private int _searchTimeout = 10;
    [ObservableProperty] private string _selectedKeyFormat = "open-key";

    // Display names → internal values used by MusicalKeyFormat.from()
    public string[] KeyFormats { get; } = ["open-key", "camelot", "standard"];

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelDownloadCommand))]
    private bool _isDownloading = false;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _successCount = 0;
    [ObservableProperty] private int _failedCount = 0;

    public ObservableCollection<TrackStatusViewModel> Tracks { get; } = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public DownloadViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _downloadService = new DownloadService();
        LoadFromSettings();
    }

    public void Dispose()
    {
        _downloadService.Dispose();
        _cts?.Dispose();
    }

    private void LoadFromSettings()
    {
        AppSettings settings = _settingsService.Load();
        SoulseekUsername = settings.SoulseekUsername;
        SoulseekPassword = SettingsService.Decrypt(settings.SoulseekPasswordEncrypted);
        SpotifyUserId = settings.LastSpotifyUserId;
        SetId3Tags = settings.SetId3Tags;
        AllowFlac = settings.AllowFlac;
        SkipExistingResults = settings.SkipExistingResults;
        SearchTimeout = settings.SearchTimeout;
        SelectedKeyFormat = settings.KeyFormat;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task BrowseAppleMusicXml()
    {
        // Avalonia file picker is invoked from the view via interaction — set path here
        // The view code-behind calls this after getting the path from StorageProvider
    }

    public void SetAppleMusicXmlPath(string path) => AppleMusicXmlPath = path;

    [RelayCommand(CanExecute = nameof(CanStartDownload))]
    private async Task StartDownload()
    {
        Tracks.Clear();
        SuccessCount = 0;
        FailedCount = 0;
        IsDownloading = true;
        StatusMessage = "Connecting...";
        _cts = new CancellationTokenSource();

        // Persist last-used settings
        AppSettings settings = _settingsService.Load();
        settings.LastSpotifyUserId = SpotifyUserId;
        settings.SetId3Tags = SetId3Tags;
        settings.AllowFlac = AllowFlac;
        settings.SkipExistingResults = SkipExistingResults;
        settings.SearchTimeout = SearchTimeout;
        settings.KeyFormat = SelectedKeyFormat;
        _settingsService.Save(settings);

        var progress = new Progress<TrackDownloadProgress>(p =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var existing = Tracks.FirstOrDefault(t => t.TrackName == p.TrackName);
                if (existing != null)
                {
                    existing.Status = p.Status;
                    existing.FailReason = p.FailReason;
                }
                else
                {
                    Tracks.Add(new TrackStatusViewModel
                    {
                        TrackName = p.TrackName,
                        Status = p.Status,
                        FailReason = p.FailReason
                    });
                }

                SuccessCount = Tracks.Count(t => t.Status == DownloadStatus.Success);
                FailedCount = Tracks.Count(t => t.Status == DownloadStatus.Failed);
                StatusMessage = $"Downloading... {SuccessCount} done, {FailedCount} failed";
            });
        });

        try
        {
            MusicalKeyFormat keyFormat = MusicalKeyFormat.from(SelectedKeyFormat);

            switch (SelectedSource)
            {
                case DownloadSource.SpotifyPlaylist:
                    await _downloadService.DownloadAllPlaylistTracksFromUserAsync(
                        SpotifyUserId,
                        string.IsNullOrWhiteSpace(SpotifyPlaylistId) ? null : SpotifyPlaylistId,
                        string.IsNullOrWhiteSpace(SpotifyPlaylistName) ? null : SpotifyPlaylistName,
                        SoulseekUsername,
                        SoulseekPassword,
                        SpotifyAccessToken,
                        SetId3Tags,
                        keyFormat,
                        opts =>
                        {
                            opts.AllowFlac = AllowFlac;
                            opts.SkipResults = SkipExistingResults;
                            opts.SearchTimeout = SearchTimeout;
                        },
                        progress);
                    break;

                case DownloadSource.AppleMusicPlaylist:
                    await _downloadService.DownloadAppleMusicPlaylistAsync(
                        AppleMusicXmlPath,
                        string.IsNullOrWhiteSpace(AppleMusicPlaylistId) ? null : AppleMusicPlaylistId,
                        string.IsNullOrWhiteSpace(AppleMusicPlaylistName) ? null : AppleMusicPlaylistName,
                        SoulseekUsername,
                        SoulseekPassword,
                        opts =>
                        {
                            opts.AllowFlac = AllowFlac;
                            opts.SkipResults = SkipExistingResults;
                            opts.SearchTimeout = SearchTimeout;
                        },
                        progress);
                    break;
            }

            StatusMessage = $"Finished — {SuccessCount} downloaded, {FailedCount} failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private bool CanStartDownload() => !IsDownloading;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelDownload()
    {
        _cts?.Cancel();
        StatusMessage = "Cancelling...";
    }

    private bool CanCancel() => IsDownloading;
}
