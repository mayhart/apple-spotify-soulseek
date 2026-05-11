using Avalonia.Controls;

namespace Spotify.Slsk.Integration.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}
