using System.IO;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace LumaWallpaper;

internal sealed class DirectCompositionWallpaper : IDisposable
{
    private readonly object _frameLock = new();
    private readonly Action<string> _playbackFailed;
    private readonly Action _firstFramePresented;
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGIFactory2 _factory;
    private readonly IDCompositionDevice _compositionDevice;
    private readonly IDCompositionTarget _compositionTarget;
    private readonly IDCompositionVisual _visual;
    private readonly IDXGISwapChain1 _swapChain;
    private readonly ID3D11RenderTargetView _renderTarget;
    private readonly IDirect3DSurface _videoSurface;
    private MediaPlayer? _player;
    private MediaSource? _source;
    private int _failureReported;
    private int _firstFrameReported;
    private bool _disposed;

    public DirectCompositionWallpaper(
        IntPtr window,
        string path,
        Action<string> playbackFailed,
        Action firstFramePresented)
    {
        _playbackFailed = playbackFailed;
        _firstFramePresented = firstFramePresented;

        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport,
            null,
            out ID3D11Device? device).CheckError();
        _device = device ?? throw new InvalidOperationException("No se pudo crear el dispositivo de video.");
        _context = _device.ImmediateContext;
        using (var multithread = _context.QueryInterfaceOrNull<ID3D11Multithread>())
            multithread?.SetMultithreadProtected(true);

        using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetAdapter();
        _factory = adapter.GetParent<IDXGIFactory2>();
        _compositionDevice = DComp.DCompositionCreateDevice<IDCompositionDevice>(dxgiDevice);
        _compositionDevice.CreateTargetForHwnd(window, true, out _compositionTarget);
        _visual = _compositionDevice.CreateVisual();

        var (width, height) = DesktopInterop.GetVirtualScreenSize();
        _swapChain = _factory.CreateSwapChainForComposition(_device, new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.B8G8R8A8_UNorm,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipSequential,
            Scaling = Scaling.Stretch,
            AlphaMode = AlphaMode.Ignore
        });

        using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTarget = _device.CreateRenderTargetView(backBuffer);
        using (var surface = backBuffer.QueryInterface<IDXGISurface>())
            _videoSurface = Direct3DSurfaceInterop.CreateSurfaceFromDxgi(surface.NativePointer);

        _visual.SetContent(_swapChain);
        _compositionTarget.SetRoot(_visual);
        _compositionDevice.Commit();

        _context.ClearRenderTargetView(_renderTarget, new Color4(0f, 0f, 0f, 1f));
        _swapChain.Present(0, PresentFlags.None).CheckError();

        _player = new MediaPlayer
        {
            IsVideoFrameServerEnabled = true,
            IsLoopingEnabled = true,
            IsMuted = true,
            Volume = 0,
            RealTimePlayback = true
        };
        _player.CommandManager.IsEnabled = false;
        _player.VideoFrameAvailable += OnVideoFrameAvailable;
        _player.MediaFailed += OnMediaFailed;
        _player.MediaEnded += OnMediaEnded;
        _source = MediaSource.CreateFromUri(new Uri(Path.GetFullPath(path)));
        _player.Source = _source;
        _player.Play();
    }

    public bool IsPaused => _player?.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;

    public void Pause() => _player?.Pause();

    public void Resume() => _player?.Play();

    private void OnVideoFrameAvailable(MediaPlayer sender, object args)
    {
        lock (_frameLock)
        {
            if (_disposed)
                return;

            try
            {
                sender.CopyFrameToVideoSurface(_videoSurface);
                _swapChain.Present(0, PresentFlags.None).CheckError();
                if (Interlocked.Exchange(ref _firstFrameReported, 1) == 0)
                    _firstFramePresented();
            }
            catch (Exception error)
            {
                ReportPlaybackFailure(error.Message);
            }
        }
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) =>
        ReportPlaybackFailure($"{args.Error}: {args.ErrorMessage}");

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        if (_disposed)
            return;

        try
        {
            sender.PlaybackSession.Position = TimeSpan.Zero;
            sender.Play();
        }
        catch (Exception error)
        {
            ReportPlaybackFailure(error.Message);
        }
    }

    private void ReportPlaybackFailure(string message)
    {
        if (Interlocked.Exchange(ref _failureReported, 1) == 0)
            _playbackFailed(message);
    }

    public void Dispose()
    {
        lock (_frameLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_player is not null)
            {
                _player.VideoFrameAvailable -= OnVideoFrameAvailable;
                _player.MediaFailed -= OnMediaFailed;
                _player.MediaEnded -= OnMediaEnded;
                _player.Pause();
                _player.Source = null;
                _source?.Dispose();
                _source = null;
                _player.Dispose();
                _player = null;
            }

            _videoSurface.Dispose();
            _renderTarget.Dispose();
            _swapChain.Dispose();
            _visual.Dispose();
            _compositionTarget.Dispose();
            _compositionDevice.Dispose();
            _factory.Dispose();
            _context.Dispose();
            _device.Dispose();
        }
    }
}

internal static class Direct3DSurfaceInterop
{
    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface", ExactSpelling = true)]
    private static extern int CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr inspectable);

    public static IDirect3DSurface CreateSurfaceFromDxgi(IntPtr dxgiSurface)
    {
        Marshal.ThrowExceptionForHR(CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface, out var inspectable));
        try
        {
            return WinRT.MarshalInterface<IDirect3DSurface>.FromAbi(inspectable);
        }
        finally
        {
            Marshal.Release(inspectable);
        }
    }
}
