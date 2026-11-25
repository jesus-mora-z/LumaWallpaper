using Microsoft.Win32;
using System.IO;

namespace LumaWallpaper;

internal static class AppSettings
{
    private const string SettingsKey = @"Software\LumaWallpaper";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "LumaWallpaper";

    public static bool InitializeAutoStart()
    {
        using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey, writable: true)
            ?? throw new IOException("No se pudo abrir la configuración de Luma Wallpaper.");

        if (settings.GetValue("AutoStartConfigured") is null)
        {
            SetAutoStart(true);
            settings.SetValue("AutoStartConfigured", 1, RegistryValueKind.DWord);
        }

        string? command;
        using (var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: false))
            command = run?.GetValue(RunValueName) as string;

        if (command is null)
            return false;

        if (!string.Equals(command, GetAutoStartCommand(), StringComparison.OrdinalIgnoreCase))
            SetAutoStart(true);

        return true;
    }

    public static string GetLanguage()
    {
        using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey, writable: false);
        return Localization.NormalizeLanguage(settings?.GetValue("Language") as string);
    }

    public static void SetLanguage(string language)
    {
        using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey, writable: true)
            ?? throw new IOException("No se pudo guardar el idioma de Luma Wallpaper.");
        settings.SetValue("Language", Localization.NormalizeLanguage(language), RegistryValueKind.String);
    }

    public static bool GetPauseOnFullscreen()
    {
        using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey, writable: false);
        return settings?.GetValue("PauseOnFullscreen") is not int value || value != 0;
    }

    public static void SetPauseOnFullscreen(bool enabled)
    {
        using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey, writable: true)
            ?? throw new IOException("No se pudo guardar la configuración de ahorro de recursos.");
        settings.SetValue("PauseOnFullscreen", enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    public static void SetAutoStart(bool enabled)
    {
        using var run = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new IOException("No se pudo actualizar el inicio automático de Windows.");

        if (!enabled)
        {
            run.DeleteValue(RunValueName, throwOnMissingValue: false);
            return;
        }

        run.SetValue(RunValueName, GetAutoStartCommand(), RegistryValueKind.String);
    }

    private static string GetAutoStartCommand()
    {
        var executable = Path.Combine(AppContext.BaseDirectory, "LumaWallpaper.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException("No se encontró el ejecutable de Luma Wallpaper.", executable);

        return $"\"{executable}\" --background";
    }

    public static string? GetLastWallpaper()
    {
        using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey, writable: false);
        return settings?.GetValue("LastWallpaper") as string;
    }

    public static void SetLastWallpaper(string path)
    {
        using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey, writable: true)
            ?? throw new IOException("No se pudo guardar el fondo seleccionado.");
        settings.SetValue("LastWallpaper", path, RegistryValueKind.String);
    }
}
