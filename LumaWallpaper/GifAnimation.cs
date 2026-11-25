using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LumaWallpaper;

internal sealed class GifAnimation : IDisposable
{
    private readonly FileStream _stream;
    private readonly GifBitmapDecoder _decoder;
    private readonly Action<BitmapSource> _showFrame;
    private readonly DispatcherTimer _timer;
    private int _frameIndex;
    private bool _disposed;

    private GifAnimation(FileStream stream, Action<BitmapSource> showFrame)
    {
        _stream = stream;
        _decoder = new GifBitmapDecoder(
            _stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnDemand);
        _showFrame = showFrame;
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += AdvanceFrame;
    }

    public static GifAnimation Create(string path, Action<BitmapSource> showFrame)
    {
        var stream = File.OpenRead(path);
        try
        {
            var animation = new GifAnimation(stream, showFrame);
            animation.ShowCurrentFrame();
            if (animation._decoder.Frames.Count > 1)
                animation.ScheduleNextFrame();
            return animation;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private void AdvanceFrame(object? sender, EventArgs e)
    {
        if (_disposed || _decoder.Frames.Count == 0)
            return;

        _frameIndex = (_frameIndex + 1) % _decoder.Frames.Count;
        ShowCurrentFrame();
        ScheduleNextFrame();
    }

    private void ShowCurrentFrame()
    {
        _showFrame(_decoder.Frames[_frameIndex]);
    }

    private void ScheduleNextFrame()
    {
        var delay = TimeSpan.FromMilliseconds(100);
        if (_decoder.Frames[_frameIndex].Metadata is BitmapMetadata metadata
            && metadata.ContainsQuery("/grctlext/Delay")
            && metadata.GetQuery("/grctlext/Delay") is ushort centiseconds)
        {
            delay = TimeSpan.FromMilliseconds(Math.Max(34, centiseconds * 10));
        }

        _timer.Interval = delay;
        _timer.Start();
    }

    public void Pause() => _timer.Stop();

    public void Resume()
    {
        if (_decoder.Frames.Count > 1)
            ScheduleNextFrame();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= AdvanceFrame;
        _stream.Dispose();
    }
}
