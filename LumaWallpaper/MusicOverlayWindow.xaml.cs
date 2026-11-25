using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LumaWallpaper;

public partial class MusicOverlayWindow : Window
{
    private const int OverlayWidth = 360;
    private const int OverlayHeight = 100;
    private const int OverlayMargin = 24;

    public MusicOverlayWindow(bool showFrame)
    {
        InitializeComponent();
        SetFrameVisible(showFrame);
    }

    internal void SetFrameVisible(bool visible)
    {
        CardFrame.Background = visible
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0xE9, 0x1A, 0x1F, 0x2A))
            : System.Windows.Media.Brushes.Transparent;
        CardFrame.BorderBrush = visible
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x74, 0x3C, 0x45, 0x55))
            : System.Windows.Media.Brushes.Transparent;
        CardFrame.BorderThickness = visible ? new Thickness(1) : new Thickness(0);
        CardFrame.Padding = visible ? new Thickness(12) : new Thickness(0);
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

        DesktopInterop.PositionOverlayOnDesktop(
            new WindowInteropHelper(this).Handle,
            OverlayWidth,
            OverlayHeight,
            OverlayMargin);
    }
}
