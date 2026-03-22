using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spotify.Slsk.Integration.Desktop.Models;
using Spotify.Slsk.Integration.Desktop.Services;

namespace Spotify.Slsk.Integration.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty] private string _soulseekUsername = string.Empty;
    [ObservableProperty] private string _soulseekPassword = string.Empty;
    [ObservableProperty] private string _savedMessage = string.Empty;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        AppSettings settings = _settingsService.Load();
        SoulseekUsername = settings.SoulseekUsername;
        SoulseekPassword = SettingsService.Decrypt(settings.SoulseekPasswordEncrypted);
    }

    [RelayCommand]
    private void Save()
    {
        AppSettings settings = _settingsService.Load();
        settings.SoulseekUsername = SoulseekUsername;
        settings.SoulseekPasswordEncrypted = SettingsService.Encrypt(SoulseekPassword);
        _settingsService.Save(settings);
        SavedMessage = "Settings saved!";
    }
}
