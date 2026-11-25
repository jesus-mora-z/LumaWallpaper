using System.Windows;
using System.Runtime.InteropServices;

namespace LumaWallpaper;

public partial class App : System.Windows.Application
{
    private Mutex? _instanceMutex;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Localization.SetLanguage(AppSettings.GetLanguage());
        var mutex = new Mutex(true, @"Local\LumaWallpaper", out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            var existingWindow = FindWindow(null, "Luma Wallpaper");
            if (existingWindow == IntPtr.Zero
                || !PostMessage(existingWindow, LumaWallpaper.MainWindow.ActivateWindowMessage, IntPtr.Zero, IntPtr.Zero))
            {
                System.Windows.MessageBox.Show(Localization.Get("StatusRunningAlready"),
                    "Luma Wallpaper", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Shutdown();
            return;
        }

        _instanceMutex = mutex;
        var window = new MainWindow();
        MainWindow = window;

        if (e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase))
            window.StartInBackground();
        else
            window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        base.OnExit(e);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
