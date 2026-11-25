# Luma Wallpaper

[English](README.en.md) | [Español](README.md)

Turn your own GIFs and MP4 videos into animated Windows wallpapers. Luma loops
them silently behind your desktop icons, can restore your last wallpaper, and
can start with Windows.

## Download

- [Download the latest version from GitHub Releases](https://github.com/jesus-mora-z/LumaWallpaper/releases/latest)
- [Older version 1.0.0 on MediaFire](https://www.mediafire.com/file/fvr62lncjaik3ma/LumaWallpaper-Setup-1.0.0-x64.exe/file)

The Windows x64 installer bundles .NET and does not require administrator
privileges. The installer is not digitally signed, so Windows may show a
SmartScreen warning.

## Features

- Use your own `.gif` and `.mp4` files.
- Loop videos without audio.
- Keep the wallpaper running from the system tray.
- Optionally remember the last wallpaper and start with Windows.
- Pauses the wallpaper while a fullscreen app is active to reduce CPU and GPU
  usage; this option can be turned off.
- Pauses the preview when the app window is hidden or minimized.
- Can show the active Windows media session's cover art, title, and artist in
  the top-right corner. YouTube Music works when its browser or app publishes
  this metadata.
- Includes Spanish, English, Brazilian Portuguese, French, German, Italian, and
  Japanese; your language choice is saved for the next session.
- Use DirectComposition and hardware video decoding on supported computers.
- If a video cannot be played, create a cached H.264 copy at up to 1080p and
  30 FPS without modifying the original file.
- Keep settings and the conversion cache local to your Windows account.

MP4/H.264 is recommended for longer wallpapers. Converting incompatible files,
including GIFs on some computers, may temporarily increase resource usage.

## Install and use

1. Download and run `LumaWallpaper-Setup-1.3.1-x64.exe`.
2. Open Luma Wallpaper from the shortcut created by the installer.
3. Select a `.gif` or `.mp4` file and click **Apply to desktop**.

Closing the window does not exit Luma; it keeps running in the system tray.
Right-click the tray icon to reopen the window or exit. Use **Iniciar con
Windows** (Start with Windows) to control automatic startup. Use **Mostrar
música en el escritorio** (Show music on desktop) to toggle the media card.

## Build the installer

You need the .NET 10 SDK and Inno Setup 6. From PowerShell, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-installer.ps1
```

The installer is created at `dist\LumaWallpaper-Setup-1.3.1-x64.exe`. The
published app is self-contained, so target computers do not need a separate
.NET installation. To run the project during development, use
`Iniciar Luma Wallpaper.bat`.

## Compatibility and licensing

Luma targets Windows 10 and 11 x64. DirectComposition is used on modern Windows
11 desktops; the Windows media player is retained on other versions.

Video conversion uses FFmpeg, distributed under the LGPL, and Windows Media
Foundation H.264. Third-party notices and licenses are included with the
installer and in `LumaWallpaper\ffmpeg\licenses`. Luma's own code does not yet
have a reuse license; no explicit permission to copy or redistribute it is
granted.

## Help

Found a problem or have a suggestion? [Open an issue on
GitHub](https://github.com/jesus-mora-z/LumaWallpaper/issues).

---

[Versión en español](README.md)
