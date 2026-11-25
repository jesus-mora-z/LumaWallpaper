using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;
using Forms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace LumaWallpaper;

public partial class MainWindow : Window
{
    internal const int ActivateWindowMessage = 0x8000 + 0x3A1;

    private string? _selectedPath;
    private string? _sourcePath;
    private string? _previewVideoPath;
    private WallpaperWindow? _wallpaper;
    private GifAnimation? _previewGif;
    private NowPlayingService? _nowPlayingService;
    private MusicOverlayWindow? _musicOverlay;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly Forms.ContextMenuStrip _trayMenu;
    private readonly Forms.ToolStripMenuItem _trayOpenItem;
    private readonly Forms.ToolStripMenuItem _trayExitItem;
    private readonly System.Windows.Threading.DispatcherTimer _fullscreenTimer;
    private bool _loadingSettings = true;
    private bool _allowClose;
    private bool _handlingPlaybackFailure;
    private bool _selectedFileIsConverted;
    private string? _statusKey = "Ready";
    private object?[] _statusArguments = [];

    public MainWindow()
    {
        InitializeComponent();
        Localization.SetLanguage(AppSettings.GetLanguage());
        LanguageComboBox.ItemsSource = Localization.Languages;
        LanguageComboBox.SelectedValuePath = nameof(LanguageOption.Code);
        LanguageComboBox.SelectedValue = Localization.Language;
        PauseOnFullscreenCheckBox.IsChecked = AppSettings.GetPauseOnFullscreen();
        ShowNowPlayingCheckBox.IsChecked = AppSettings.GetShowNowPlaying();
        ShowNowPlayingFrameCheckBox.IsChecked = AppSettings.GetShowNowPlayingFrame();
        _fullscreenTimer = new System.Windows.Threading.DispatcherTimer(
            System.Windows.Threading.DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _fullscreenTimer.Tick += FullscreenTimer_Tick;
        var handle = new WindowInteropHelper(this).EnsureHandle();
        HwndSource.FromHwnd(handle)?.AddHook(WindowMessageHook);

        _trayMenu = new Forms.ContextMenuStrip();
        _trayOpenItem = new Forms.ToolStripMenuItem();
        _trayOpenItem.Click += (_, _) => ShowWindow();
        _trayExitItem = new Forms.ToolStripMenuItem();
        _trayExitItem.Click += (_, _) => ExitApplication();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = Localization.Get("TrayIconText"),
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayMenu.Items.AddRange([_trayOpenItem, _trayExitItem]);
        ApplyLanguage();
        _trayIcon.DoubleClick += (_, _) => Dispatcher.BeginInvoke(new Action(ShowWindow));

        try
        {
            AutoStartCheckBox.IsChecked = AppSettings.InitializeAutoStart();
        }

        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            AutoStartCheckBox.IsChecked = false;
            SetStatus("StatusSettingsFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogStartupSettings"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _loadingSettings = false;
        Closed += (_, _) =>
        {
            _fullscreenTimer.Stop();
            StopPreview();
            StopNowPlaying();
            RemoveWallpaper();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        };
    }

    public void StartInBackground()
    {
        try
        {
            _selectedPath = AppSettings.GetLastWallpaper();
            _sourcePath = _selectedPath;
            if (string.IsNullOrWhiteSpace(_sourcePath))
            {
                SetStatus("StatusBackgroundWaiting");
                return;
            }

            if (!File.Exists(_sourcePath))
            {
                SetStatus("StatusSavedMissing");
                return;
            }

            SetSelectedFile(_sourcePath);
            _selectedPath = VideoTranscoder.GetCachedPath(_sourcePath) ?? _sourcePath;
            ApplySelectedWallpaper();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            SetStatus("StatusRestoreFailed");
            _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                error.Message, Forms.ToolTipIcon.Error);
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loadingSettings || LanguageComboBox.SelectedValue is not string language)
            return;

        var previousLanguage = Localization.Language;
        try
        {
            AppSettings.SetLanguage(language);
            Localization.SetLanguage(language);
            ApplyLanguage();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _loadingSettings = true;
            LanguageComboBox.SelectedValue = previousLanguage;
            _loadingSettings = false;
            SetStatus("StatusLanguageSaveFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogLanguageSaveFailed"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyLanguage()
    {
        Title = Localization.Get("TrayIconText");
        TaglineText.Text = Localization.Get("Tagline");
        SubtitleText.Text = Localization.Get("Subtitle");
        LanguageLabel.Text = Localization.Get("Language");
        PreviewHeadingText.Text = Localization.Get("PreviewHeading");
        PreviewPlaceholder.Text = Localization.Get("PreviewPlaceholder");
        CurrentWallpaperText.Text = Localization.Get("CurrentWallpaper");
        ChooseFileButton.Content = Localization.Get("ChooseFile");
        ApplyButton.Content = Localization.Get("Apply");
        RemoveButton.Content = Localization.Get("Remove");
        AutoStartCheckBox.Content = Localization.Get("AutoStart");
        PauseOnFullscreenCheckBox.Content = Localization.Get("PauseOnFullscreen");
        ShowNowPlayingCheckBox.Content = Localization.Get("ShowNowPlaying");
        ShowNowPlayingFrameCheckBox.Content = Localization.Get("ShowNowPlayingFrame");
        TipText.Text = Localization.Get("Tip");
        _trayOpenItem.Text = Localization.Get("TrayOpen");
        _trayExitItem.Text = Localization.Get("TrayExit");
        _trayIcon.Text = Localization.Get("TrayIconText");
        UpdatePauseButton();
        UpdateSelectedFileText();
        StatusText.Text = Localization.Get(_statusKey ?? "Ready", _statusArguments);
    }

    private void SetStatus(string key, params object?[] arguments)
    {
        _statusKey = key;
        _statusArguments = arguments;
        StatusText.Text = Localization.Get(key, arguments);
    }

    private void SetSelectedFile(string path, bool converted = false)
    {
        _sourcePath = path;
        _selectedFileIsConverted = converted;
        UpdateSelectedFileText();
    }

    private void UpdateSelectedFileText()
    {
        SelectedFileText.Text = _sourcePath is null
            ? Localization.Get("NoFileSelected")
            : _selectedFileIsConverted
                ? Localization.Get("ConvertedFileName", Path.GetFileName(_sourcePath))
                : Path.GetFileName(_sourcePath);
    }

    private void UpdatePauseButton() =>
        PauseButton.Content = Localization.Get(_wallpaper?.IsPaused == true ? "Resume" : "Pause");

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || AutoStartCheckBox.IsChecked is not bool enabled)
            return;

        try
        {
            AppSettings.SetAutoStart(enabled);
            SetStatus(enabled ? "StatusAutoEnabled" : "StatusAutoDisabled");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _loadingSettings = true;
            AutoStartCheckBox.IsChecked = !enabled;
            _loadingSettings = false;
            SetStatus("StatusAutoFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogStartupSettings"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PauseOnFullscreenCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || PauseOnFullscreenCheckBox.IsChecked is not bool enabled)
            return;

        try
        {
            AppSettings.SetPauseOnFullscreen(enabled);
            if (enabled)
            {
                UpdateFullscreenPause();
                SetStatus(_wallpaper?.IsPausedAutomatically == true
                    ? "StatusFullscreenPaused"
                    : "StatusFullscreenPauseEnabled");
            }
            else
            {
                _wallpaper?.SetAutomaticPause(false);
                UpdatePauseButton();
                SetStatus("StatusFullscreenPauseDisabled");
            }

            UpdateFullscreenTimer();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _loadingSettings = true;
            PauseOnFullscreenCheckBox.IsChecked = !enabled;
            _loadingSettings = false;
            SetStatus("StatusFullscreenPauseFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogResourceSettings"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowNowPlayingCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || ShowNowPlayingCheckBox.IsChecked is not bool enabled)
            return;

        try
        {
            AppSettings.SetShowNowPlaying(enabled);
            if (enabled)
            {
                if (_wallpaper is not null)
                    _ = StartNowPlayingAsync();
                SetStatus("StatusMusicEnabled");
            }
            else
            {
                StopNowPlaying();
                SetStatus("StatusMusicDisabled");
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _loadingSettings = true;
            ShowNowPlayingCheckBox.IsChecked = !enabled;
            _loadingSettings = false;
            SetStatus("StatusMusicFailed", error.Message);
            MessageBox.Show(this, error.Message, Localization.Get("DialogResourceSettings"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowNowPlayingFrameCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings || ShowNowPlayingFrameCheckBox.IsChecked is not bool enabled)
            return;

        try
        {
            AppSettings.SetShowNowPlayingFrame(enabled);
            _musicOverlay?.SetFrameVisible(enabled);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _loadingSettings = true;
            ShowNowPlayingFrameCheckBox.IsChecked = !enabled;
            _loadingSettings = false;
            SetStatus("StatusMusicFrameFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogResourceSettings"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Localization.Get("StatusFileTitle"),
            Filter = $"{Localization.Get("FileDialogAll")}|*.gif;*.mp4|{Localization.Get("FileDialogGif")}|*.gif|{Localization.Get("FileDialogMp4")}|*.mp4",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            StopPreview();
            _selectedPath = dialog.FileName;
            _sourcePath = _selectedPath;
            SetSelectedFile(_selectedPath);
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            if (IsGif(_selectedPath))
            {
                PreviewVideo.Visibility = Visibility.Collapsed;
                PreviewGif.Visibility = Visibility.Visible;
                PreviewGif.Source = null;
                _previewGif = GifAnimation.Create(_selectedPath, frame =>
                    PreviewGif.Dispatcher.Invoke(() => PreviewGif.Source = frame));
            }
            else
            {
                PreviewGif.Source = null;
                PreviewGif.Visibility = Visibility.Collapsed;
                PreviewVideo.Visibility = Visibility.Visible;
                _previewVideoPath = _selectedPath;
                PreviewVideo.Source = new Uri(_selectedPath, UriKind.Absolute);
            }

            SetStatus(_wallpaper is null ? "StatusPreviewReady" : "StatusPreviewChanged");
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException or FormatException)
        {
            StopPreview();
            SetStatus("StatusOpenFailed");
            MessageBox.Show(this, error.Message, Localization.Get("DialogOpenFailed"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedPath))
        {
            MessageBox.Show(this, Localization.Get("StatusChooseFirst"),
                Localization.Get("DialogChooseWallpaper"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ApplySelectedWallpaper();
    }

    private bool ApplySelectedWallpaper()
    {
        if (string.IsNullOrWhiteSpace(_selectedPath))
            return false;

        WallpaperWindow? nextWallpaper = null;
        try
        {
            var sourcePath = _sourcePath ?? _selectedPath;
            var playbackPath = VideoTranscoder.GetCachedPath(sourcePath) ?? _selectedPath;
            nextWallpaper = new WallpaperWindow(
                playbackPath,
                ShowPlaybackError,
                () => Dispatcher.BeginInvoke(new Action(() => MarkWallpaperReady(sourcePath))));
            nextWallpaper.ShowOnDesktop();
            AppSettings.SetLastWallpaper(sourcePath);
            RemoveWallpaper();
            _wallpaper = nextWallpaper;
            nextWallpaper = null;
            UpdateFullscreenTimer();
            UpdateFullscreenPause();
            StopPreview();
            UpdatePauseButton();
            SetStatus("StatusApplying", Path.GetFileName(sourcePath));
            PositionMusicOverlay();
            if (ShowNowPlayingCheckBox.IsChecked == true)
                _ = StartNowPlayingAsync();
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException or ArgumentException or FormatException or System.ComponentModel.Win32Exception)
        {
            nextWallpaper?.Close();
            SetStatus("StatusApplyFailed");
            if (IsVisible)
                MessageBox.Show(this, error.Message, Localization.Get("DialogApplyFailed"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            else
                _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                    error.Message, Forms.ToolTipIcon.Error);
            return false;
        }
    }

    private void MarkWallpaperReady(string sourcePath)
    {
        if (!string.Equals(_sourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
            return;

        SetStatus("StatusWallpaperVisible", Path.GetFileName(sourcePath));
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (_wallpaper is null)
        {
            SetStatus("StatusPauseFirst");
            return;
        }

        if (_wallpaper.IsPaused)
        {
            _wallpaper.Resume();
            UpdatePauseButton();
            SetStatus(_wallpaper.IsPaused
                ? "StatusFullscreenPaused"
                : "StatusWallpaperMoving");
        }
        else
        {
            _wallpaper.Pause();
            UpdatePauseButton();
            SetStatus("StatusWallpaperPaused");
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        RemoveWallpaper();
        StopNowPlaying();
        UpdatePauseButton();
        SetStatus("StatusWallpaperRemoved");
    }

    private void PreviewVideo_MediaOpened(object sender, RoutedEventArgs e)
    {
        PreviewVideo.Play();
        SetStatus("StatusVideoReady", Path.GetFileName(_sourcePath ?? _selectedPath));
    }

    private void PreviewVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        PreviewVideo.Position = TimeSpan.Zero;
        PreviewVideo.Play();
    }

    private async void PreviewVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        var failedPath = _previewVideoPath;
        StopPreview();
        if (failedPath is null || IsCachedVideo(failedPath))
        {
            ShowVideoError(e.ErrorException.Message);
            return;
        }

        await ConvertAndPreviewAsync(failedPath);
    }

    private void ShowPlaybackError(string message)
    {
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            if (_handlingPlaybackFailure)
                return;

            var sourcePath = _sourcePath;
            if (sourcePath is not null
                && (IsVideo(sourcePath) || IsGif(sourcePath))
                && VideoTranscoder.GetCachedPath(sourcePath) is null)
            {
                _handlingPlaybackFailure = true;
                try
                {
                    var convertedPath = await ConvertVideoAsync(sourcePath);
                    if (convertedPath is not null)
                    {
                        _selectedPath = convertedPath;
                        SetSelectedFile(sourcePath, converted: true);
                        RemoveWallpaper();
                        StopNowPlaying();
                        ApplySelectedWallpaper();
                        return;
                    }
                }
                finally
                {
                    _handlingPlaybackFailure = false;
                }
            }

            RemoveWallpaper();
            StopNowPlaying();
            ShowVideoError(message);
        }));
    }

    private async Task ConvertAndPreviewAsync(string sourcePath)
    {
        var convertedPath = await ConvertVideoAsync(sourcePath);
        if (convertedPath is null || !string.Equals(_sourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
            return;

        _selectedPath = convertedPath;
        SetSelectedFile(sourcePath, converted: true);
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        PreviewVideo.Visibility = Visibility.Visible;
        _previewVideoPath = convertedPath;
        PreviewVideo.Source = new Uri(convertedPath, UriKind.Absolute);
        SetStatus("StatusConversionReady");
    }

    private async Task<string?> ConvertVideoAsync(string sourcePath)
    {
        SetStatus("StatusConverting", Path.GetFileName(sourcePath));
        try
        {
            var progress = new Progress<double>(value =>
                SetStatus("StatusConvertingProgress", value.ToString("P0", Localization.Culture)));
            return await VideoTranscoder.ConvertToH264Async(sourcePath, progress);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            SetStatus("StatusConversionFailed");
            var message = Localization.Get("ConversionDetails", error.Message);
            if (IsVisible)
                MessageBox.Show(this, message, Localization.Get("DialogConversionFailed"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            else
                _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                    Localization.Get("BalloonConversionFailed"),
                    Forms.ToolTipIcon.Error);
            return null;
        }
    }

    private void ShowVideoError(string message)
    {
        SetStatus("StatusPlaybackFailed");
        var details = Localization.Get("PlaybackDetails", message);
        if (IsVisible)
            MessageBox.Show(this, details, Localization.Get("DialogPlaybackError"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        else
            _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                details, Forms.ToolTipIcon.Error);
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        PausePreview();
        Hide();
        SetStatus("StatusBackgroundTray");
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            PausePreview();
        else if (IsVisible)
            ResumePreview();
    }

    private void ShowWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        ResumePreview();
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == ActivateWindowMessage)
        {
            handled = true;
            Dispatcher.BeginInvoke(new Action(ShowWindow));
        }

        return IntPtr.Zero;
    }

    private void ExitApplication()
    {
        _allowClose = true;
        StopPreview();
        StopNowPlaying();
        RemoveWallpaper();
        _trayIcon.Visible = false;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void StopPreview()
    {
        PreviewVideo.Stop();
        PreviewVideo.Source = null;
        _previewVideoPath = null;
        PreviewGif.Source = null;
        _previewGif?.Dispose();
        _previewGif = null;
        PreviewPlaceholder.Visibility = Visibility.Visible;
    }

    private void PausePreview()
    {
        if (PreviewVideo.Source is not null)
            PreviewVideo.Pause();
        _previewGif?.Pause();
    }

    private void ResumePreview()
    {
        if (PreviewVideo.Source is not null)
            PreviewVideo.Play();
        _previewGif?.Resume();
    }

    private void FullscreenTimer_Tick(object? sender, EventArgs e) =>
        UpdateFullscreenPause();

    private void UpdateFullscreenTimer()
    {
        if (_wallpaper is not null && PauseOnFullscreenCheckBox.IsChecked == true)
            _fullscreenTimer.Start();
        else
            _fullscreenTimer.Stop();
    }

    private void UpdateFullscreenPause()
    {
        if (_wallpaper is null || PauseOnFullscreenCheckBox.IsChecked != true)
            return;

        var pausedAutomatically = DesktopInterop.IsFullscreenApplicationActive();
        if (_wallpaper.IsPausedAutomatically == pausedAutomatically)
            return;

        _wallpaper.SetAutomaticPause(pausedAutomatically);
        UpdatePauseButton();
        SetStatus(pausedAutomatically
            ? "StatusFullscreenPaused"
            : _wallpaper.IsPaused
                ? "StatusWallpaperPaused"
                : "StatusWallpaperMoving");
    }

    private async Task StartNowPlayingAsync()
    {
        if (_wallpaper is null || ShowNowPlayingCheckBox.IsChecked != true
            || _nowPlayingService is not null)
            return;

        var service = new NowPlayingService(Dispatcher);
        _nowPlayingService = service;
        _musicOverlay = new MusicOverlayWindow(AppSettings.GetShowNowPlayingFrame());
        service.TrackChanged += track =>
        {
            if (_wallpaper is not null)
            {
                try
                {
                    _musicOverlay?.ShowTrack(track);
                }
                catch (Exception error) when (error is InvalidOperationException
                    or System.ComponentModel.Win32Exception)
                {
                    SetStatus("StatusMusicFailed", error.Message);
                }
            }
        };
        service.Failed += message =>
        {
            SetStatus("StatusMusicFailed", message);
            _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                Localization.Get("StatusMusicFailed", message),
                Forms.ToolTipIcon.Error);
        };

        try
        {
            await service.StartAsync();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if (ReferenceEquals(_nowPlayingService, service))
                StopNowPlaying();
            SetStatus("StatusMusicFailed", error.Message);
            _trayIcon.ShowBalloonTip(5000, Localization.Get("TrayIconText"),
                Localization.Get("StatusMusicFailed", error.Message),
                Forms.ToolTipIcon.Error);
        }
    }

    private void StopNowPlaying()
    {
        _nowPlayingService?.Dispose();
        _nowPlayingService = null;
        if (_musicOverlay is not null)
        {
            _musicOverlay.Close();
            _musicOverlay = null;
        }
    }

    private void PositionMusicOverlay()
    {
        try
        {
            _musicOverlay?.PositionOnDesktop();
        }
        catch (Exception error) when (error is InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            SetStatus("StatusMusicFailed", error.Message);
        }
    }

    private void RemoveWallpaper()
    {
        _fullscreenTimer.Stop();
        _wallpaper?.Close();
        _wallpaper = null;
    }

    private static bool IsGif(string path) =>
        string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase);

    private static bool IsVideo(string path) =>
        string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase);

    private static bool IsCachedVideo(string path) =>
        path.StartsWith(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LumaWallpaper", "Converted") + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
}
