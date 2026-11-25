using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LumaWallpaper;

public partial class MusicOverlayWindow : Window
{
    private const int OverlayWidth = 360;
    private const int OverlayHeight = 100;
    private const int OverlayMargin = 24;

    public MusicOverlayWindow()
    {
        InitializeComponent();
    }

    internal void ShowTrack(NowPlayingTrack? track)
    {
        if (track is null)
        {
            Hide();
            return;
        }

        TrackTitleText.Text = track.Title;
        TrackArtistText.Text = track.Artist;
        ArtworkImage.Source = track.Artwork;
        ArtworkPlaceholder.Visibility = track.Artwork is null
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!IsVisible)
            Show();

        PositionOnDesktop();
    }

    internal void PositionOnDesktop()
    {
        if (!IsVisible)
            return;

        DesktopInterop.AttachOverlayToDesktop(
            new WindowInteropHelper(this).Handle,
            OverlayWidth,
            OverlayHeight,
            OverlayMargin);
    }
}
