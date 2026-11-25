using System.IO;
using System.Windows;
using System.Windows.Interop;

namespace LumaWallpaper;

public partial class WallpaperWindow : Window
{
    private readonly string _path;
    private readonly Action<string> _playbackError;
    private readonly Action? _playbackReady;
    private readonly bool _useDirectComposition;
    private GifAnimation? _gif;
    private DirectCompositionWallpaper? _compositionWallpaper;
    private bool _isPaused;
    private bool _isPausedAutomatically;
    private bool _isClosed;
    private int _readyReported;

    public WallpaperWindow(string path, Action<string> playbackError, Action? playbackReady = null)
    {
        _path = path;
        _playbackError = playbackError;
        _playbackReady = playbackReady;
        _useDirectComposition = DesktopInterop.RequiresDirectComposition;
        InitializeComponent();

        if (!_useDirectComposition
            && string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            WallpaperGif.Visibility = Visibility.Visible;
            _gif = GifAnimation.Create(path, frame =>
            {
                WallpaperGif.Dispatcher.Invoke(() => WallpaperGif.Source = frame);
                ReportPlaybackReady();
            });
        }
        else if (!_useDirectComposition)
        {
            WallpaperVideo.Visibility = Visibility.Visible;
            WallpaperVideo.Source = new Uri(path, UriKind.Absolute);
        }

        Closed += (_, _) => ReleaseMedia();
    }

    public bool IsPaused => _isPaused || _isPausedAutomatically;

    public bool IsPausedAutomatically => _isPausedAutomatically;

    public void ShowOnDesktop()
    {
        Show();
        var handle = new WindowInteropHelper(this).Handle;
        DesktopInterop.AttachToDesktop(handle);
        if (_useDirectComposition)
        {
            _compositionWallpaper = new DirectCompositionWallpaper(
                handle, _path, _playbackError, ReportPlaybackReady);
        }

        if (_gif is not null)
            _gif.Resume();
    }

    public void Pause()
    {
        _isPaused = true;
        PausePlayback();
    }

    public void SetAutomaticPause(bool paused)
    {
        _isPausedAutomatically = paused;
        if (paused)
        {
            PausePlayback();
        }
        else if (!_isPaused)
        {
            ResumePlayback();
        }
    }

    private void PausePlayback()
    {
        _gif?.Pause();
        _compositionWallpaper?.Pause();
        if (_compositionWallpaper is null)
            WallpaperVideo.Pause();
    }

    public void Resume()
    {
        _isPaused = false;
        if (_isPausedAutomatically)
            return;

        ResumePlayback();
    }

    private void ResumePlayback()
    {
        _gif?.Resume();
        _compositionWallpaper?.Resume();
        if (_gif is null && _compositionWallpaper is null)
            WallpaperVideo.Play();
    }

    private void WallpaperVideo_MediaOpened(object sender, RoutedEventArgs e)
    {
        ReportPlaybackReady();
        if (!IsPaused)
            WallpaperVideo.Play();
    }

    private void WallpaperVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        WallpaperVideo.Position = TimeSpan.Zero;
        if (!IsPaused)
            WallpaperVideo.Play();
    }

    private void WallpaperVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (!_isClosed)
            _playbackError(e.ErrorException.Message);
    }

    private void ReportPlaybackReady()
    {
        if (Interlocked.Exchange(ref _readyReported, 1) == 0)
            _playbackReady?.Invoke();
    }

    private void ReleaseMedia()
    {
        _isClosed = true;
        _compositionWallpaper?.Dispose();
        _compositionWallpaper = null;
        WallpaperVideo.Stop();
        WallpaperVideo.Source = null;
        WallpaperGif.Source = null;
        _gif?.Dispose();
        _gif = null;
    }
}
