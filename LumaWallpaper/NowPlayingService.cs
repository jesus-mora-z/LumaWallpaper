using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace LumaWallpaper;

internal sealed record NowPlayingTrack(string Title, string Artist, BitmapSource? Artwork);

internal sealed class NowPlayingService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private bool _disposed;

    public NowPlayingService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public event Action<NowPlayingTrack?>? TrackChanged;
    public event Action<string>? Failed;

    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
        _manager.SessionsChanged += Manager_SessionsChanged;
        await RefreshAsync();
    }

    private void Manager_CurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args) =>
        _ = RefreshSafelyAsync();

    private void Manager_SessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args) =>
        _ = RefreshSafelyAsync();

    private void Session_MediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args) =>
        _ = RefreshSafelyAsync();

    private void Session_PlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args) =>
        _ = RefreshSafelyAsync();

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or InvalidOperationException or COMException)
        {
            Dispatch(() => Failed?.Invoke(error.Message));
        }
    }

    private async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            if (_disposed || _manager is null)
                return;

            var session = SelectPlayingSession(_manager);
            if (!ReferenceEquals(session, _session))
            {
                if (_session is not null)
                {
                    _session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    _session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                }

                _session = session;
                if (_session is not null)
                {
                    _session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                    _session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                }
            }

            if (session is null)
            {
                Dispatch(() => TrackChanged?.Invoke(null));
                return;
            }

            var properties = await session.TryGetMediaPropertiesAsync();
            if (_disposed || !ReferenceEquals(session, _session))
                return;

            if (string.IsNullOrWhiteSpace(properties.Title))
            {
                Dispatch(() => TrackChanged?.Invoke(null));
                return;
            }

            var artwork = await ReadArtworkAsync(properties.Thumbnail);
            var track = new NowPlayingTrack(
                properties.Title,
                properties.Artist,
                artwork);
            Dispatch(() => TrackChanged?.Invoke(track));
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static GlobalSystemMediaTransportControlsSession? SelectPlayingSession(
        GlobalSystemMediaTransportControlsSessionManager manager)
    {
        var sessions = manager.GetSessions();
        var youtubeMusic = sessions.FirstOrDefault(session =>
            IsPlayingExternalSession(session) && IsYouTubeMusicSession(session));
        if (youtubeMusic is not null)
            return youtubeMusic;

        var current = manager.GetCurrentSession();
        if (IsPlayingExternalSession(current))
            return current;

        return sessions.FirstOrDefault(IsPlayingExternalSession);
    }

    private static bool IsPlayingExternalSession(
        GlobalSystemMediaTransportControlsSession? session) =>
        session is not null
        && session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
        && !session.SourceAppUserModelId.Contains("LumaWallpaper", StringComparison.OrdinalIgnoreCase);

    private static bool IsYouTubeMusicSession(
        GlobalSystemMediaTransportControlsSession session) =>
        session.SourceAppUserModelId.Contains("music.youtube.com", StringComparison.OrdinalIgnoreCase)
        || session.SourceAppUserModelId.Contains("youtube music", StringComparison.OrdinalIgnoreCase);

    private static async Task<BitmapSource?> ReadArtworkAsync(
        IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail is null)
            return null;

        using var stream = await thumbnail.OpenReadAsync();
        if (stream.Size is 0 or > 4 * 1024 * 1024)
            return null;

        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[(int)stream.Size];
        reader.ReadBytes(bytes);

        var bitmap = new BitmapImage();
        using var imageStream = new MemoryStream(bytes);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 128;
        bitmap.StreamSource = imageStream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void Dispatch(Action callback)
    {
        if (!_dispatcher.HasShutdownStarted)
            _dispatcher.BeginInvoke(callback);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_manager is not null)
        {
            _manager.CurrentSessionChanged -= Manager_CurrentSessionChanged;
            _manager.SessionsChanged -= Manager_SessionsChanged;
            _manager = null;
        }

        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            _session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
            _session = null;
        }

        TrackChanged = null;
        Failed = null;
    }
}
