using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Spotify.Slsk.Integration.Desktop.ViewModels;

namespace Spotify.Slsk.Integration.Desktop.Views;

public partial class DownloadView : UserControl
{
    public DownloadView()
    {
        InitializeComponent();
    }

    private async void BrowseXmlFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Apple Music Library XML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("XML Files") { Patterns = ["*.xml"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        if (files.Count > 0 && DataContext is DownloadViewModel vm)
        {
            vm.SetAppleMusicXmlPath(files[0].Path.LocalPath);
        }
    }

    private async void BrowseOutputFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is DownloadViewModel vm)
        {
            vm.SetOutputFolder(folders[0].Path.LocalPath);
        }
    }
}
