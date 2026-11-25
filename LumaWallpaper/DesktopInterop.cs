using System.Runtime.InteropServices;

namespace LumaWallpaper;

internal static class DesktopInterop
{
    private const uint WorkerMessage = 0x052C;
    private const uint SmtoNormal = 0;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsChild = 0x40000000L;
    private const long WsPopup = 0x80000000L;
    private const long WsVisible = 0x10000000L;
    private const long WsExNoRedirectionBitmap = 0x00200000L;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndBottom = new(1);

    public static bool RequiresDirectComposition
    {
        get
        {
            var progman = FindTopLevelWindow("Progman");
            return progman != IntPtr.Zero
                && (GetWindowStyle(progman, GwlExStyle) & WsExNoRedirectionBitmap) != 0;
        }
    }

    public static (int Width, int Height) GetVirtualScreenSize() =>
        (GetSystemMetrics(78), GetSystemMetrics(79));

    public static bool IsFullscreenApplicationActive()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
            return false;

        var className = new System.Text.StringBuilder(128);
        GetClassName(foregroundWindow, className, className.Capacity);
        if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
            return false;

        var monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo)
            || !GetWindowRect(foregroundWindow, out var windowRect))
            return false;

        const int edgeTolerance = 8;
        var bounds = monitorInfo.Monitor;
        return windowRect.Left <= bounds.Left + edgeTolerance
            && windowRect.Top <= bounds.Top + edgeTolerance
            && windowRect.Right >= bounds.Right - edgeTolerance
            && windowRect.Bottom >= bounds.Bottom - edgeTolerance;
    }

    public static void AttachToDesktop(IntPtr window)
    {
        var progman = FindTopLevelWindow("Progman");
        if (progman == IntPtr.Zero)
            throw new InvalidOperationException("Windows no encontró la ventana del escritorio.");

        SendMessageTimeout(progman, WorkerMessage, new UIntPtr(0xD), new IntPtr(1),
            SmtoNormal, 1000, out _);

        if ((GetWindowStyle(progman, GwlExStyle) & WsExNoRedirectionBitmap) != 0)
        {
            AttachToRaisedDesktop(window, progman);
            return;
        }

        var host = FindClassicDesktopHost(progman);
        AttachWindow(window, host);
        var (width, height) = GetVirtualScreenSize();
        if (!GetWindowRect(host, out var parentRect)
            || !SetWindowPos(
                window,
                HwndBottom,
                GetSystemMetrics(76) - parentRect.Left,
                GetSystemMetrics(77) - parentRect.Top,
                width,
                height,
                SwpNoActivate | SwpFrameChanged | SwpShowWindow))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static void PositionOverlayOnDesktop(IntPtr window, int width, int height, int margin)
    {
        var x = GetSystemMetrics(0) - width - margin;
        if (!SetWindowPos(window, IntPtr.Zero, x, margin, width, height,
                SwpNoActivate | SwpFrameChanged | SwpShowWindow))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static void AttachToRaisedDesktop(IntPtr window, IntPtr progman)
    {
        var iconLayer = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        var shellWallpaperLayer = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        if (iconLayer == IntPtr.Zero)
            throw new InvalidOperationException("Windows no encontró la capa de iconos del escritorio.");

        var style = GetWindowStyle(window, GwlStyle);
        SetWindowStyle(window, (style & ~WsPopup) | WsChild | WsVisible);
        AttachWindow(window, progman);

        if (!GetWindowRect(progman, out var parentRect))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        var (width, height) = GetVirtualScreenSize();
        if (!SetWindowPos(
                window,
                iconLayer,
                GetSystemMetrics(76) - parentRect.Left,
                GetSystemMetrics(77) - parentRect.Top,
                width,
                height,
                SwpNoActivate | SwpFrameChanged | SwpShowWindow))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        if (shellWallpaperLayer != IntPtr.Zero
            && !SetWindowPos(shellWallpaperLayer, HwndBottom, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoActivate))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static void AttachWindow(IntPtr window, IntPtr host)
    {
        Marshal.SetLastPInvokeError(0);
        if (SetParent(window, host) == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
                throw new System.ComponentModel.Win32Exception(error);
        }

        var style = GetWindowStyle(window, GwlStyle);
        SetWindowStyle(window, (style & ~WsPopup) | WsChild | WsVisible);
    }

    private static IntPtr FindClassicDesktopHost(IntPtr progman)
    {
        IntPtr desktopHost = IntPtr.Zero;
        IntPtr worker = IntPtr.Zero;
        EnumWindows((topWindow, _) =>
        {
            if (FindWindowEx(topWindow, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero)
                return true;

            desktopHost = topWindow;
            worker = FindWindowEx(IntPtr.Zero, topWindow, "WorkerW", null);
            return worker == IntPtr.Zero;
        }, IntPtr.Zero);

        if (worker != IntPtr.Zero)
            return worker;

        if (desktopHost != IntPtr.Zero)
        {
            worker = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (worker != IntPtr.Zero)
                return worker;
        }

        if (desktopHost != IntPtr.Zero)
            return desktopHost;

        throw new InvalidOperationException("Windows no encontró la capa del escritorio.");
    }

    private static IntPtr FindTopLevelWindow(string className)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            var classBuffer = new System.Text.StringBuilder(128);
            GetClassName(window, classBuffer, classBuffer.Capacity);
            if (!string.Equals(classBuffer.ToString(), className, StringComparison.Ordinal))
                return true;

            found = window;
            return false;
        }, IntPtr.Zero);
        return found;
    }

    private static long GetWindowStyle(IntPtr window, int index) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(window, index).ToInt64() : GetWindowLong32(window, index);

    private static void SetWindowStyle(IntPtr window, long style)
    {
        Marshal.SetLastPInvokeError(0);
        if (IntPtr.Size == 8)
        {
            if (SetWindowLongPtr64(window, GwlStyle, new IntPtr(style)) == IntPtr.Zero
                && Marshal.GetLastWin32Error() != 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        else
        {
            if (SetWindowLong32(window, GwlStyle, unchecked((int)style)) == 0
                && Marshal.GetLastWin32Error() != 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, System.Text.StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr window, int index, int value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr window, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr window,
        uint message,
        UIntPtr wParam,
        IntPtr lParam,
        uint flags,
        uint timeout,
        out IntPtr result);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }
}
