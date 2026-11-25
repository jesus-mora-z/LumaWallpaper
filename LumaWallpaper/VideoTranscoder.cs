using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LumaWallpaper;

internal static class VideoTranscoder
{
    private static readonly string FfmpegDirectory =
        Path.Combine(AppContext.BaseDirectory, "ffmpeg", "win-x64");

    public static string GetCachePath(string sourcePath)
    {
        var source = new FileInfo(sourcePath);
        var identity = $"{source.FullName}|{source.Length}|{source.LastWriteTimeUtc.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LumaWallpaper",
            "Converted",
            $"{hash}.mp4");
    }

    public static string? GetCachedPath(string sourcePath)
    {
        var cachePath = GetCachePath(sourcePath);
        return File.Exists(cachePath) && new FileInfo(cachePath).Length > 0
            ? cachePath
            : null;
    }

    public static async Task<string> ConvertToH264Async(
        string sourcePath,
        IProgress<double>? progress = null)
    {
        var cachedPath = GetCachedPath(sourcePath);
        if (cachedPath is not null)
            return cachedPath;

        var destinationPath = GetCachePath(sourcePath);

        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("No se pudo determinar la carpeta de conversión.");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(destinationDirectory, $"{Guid.NewGuid():N}.mp4");

        try
        {
            var ffmpeg = Path.Combine(FfmpegDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpeg))
                throw new FileNotFoundException("No se encontró el conversor FFmpeg de Luma.", ffmpeg);

            var duration = await GetDurationAsync(sourcePath);
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                WorkingDirectory = FfmpegDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[]
            {
                "-hide_banner", "-loglevel", "error", "-y", "-i", sourcePath,
                "-an", "-vf",
                "scale=1920:1080:force_original_aspect_ratio=decrease:force_divisible_by=2,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setsar=1,fps=30",
                "-c:v", "h264_mf", "-b:v", "8M", "-pix_fmt", "yuv420p",
                "-movflags", "+faststart", "-progress", "pipe:1", "-nostats", temporaryPath
            })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var outputTask = ReadProgressAsync(process, duration, progress);
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await outputTask;
            var errors = await errorTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(errors)
                        ? $"FFmpeg terminó con código {process.ExitCode}."
                        : errors.Trim());
            if (!File.Exists(temporaryPath) || new FileInfo(temporaryPath).Length == 0)
                throw new InvalidOperationException("FFmpeg no generó un video convertido.");

            File.Move(temporaryPath, destinationPath, overwrite: true);
            return destinationPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static async Task<TimeSpan?> GetDurationAsync(string sourcePath)
    {
        var ffprobe = Path.Combine(FfmpegDirectory, "ffprobe.exe");
        if (!File.Exists(ffprobe))
            return null;

        var startInfo = new ProcessStartInfo
        {
            FileName = ffprobe,
            WorkingDirectory = FfmpegDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[]
        {
            "-v", "error", "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1", sourcePath
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await errorTask;
        return process.ExitCode == 0
            && double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : null;
    }

    private static async Task ReadProgressAsync(
        Process process,
        TimeSpan? duration,
        IProgress<double>? progress)
    {
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            if (duration is null || progress is null
                || !line.StartsWith("out_time_ms=", StringComparison.Ordinal)
                || !long.TryParse(line.AsSpan("out_time_ms=".Length), out var microseconds))
                continue;

            var fraction = TimeSpan.FromTicks(microseconds * 10).TotalSeconds
                / duration.Value.TotalSeconds;
            progress.Report(Math.Clamp(fraction, 0, 1));
        }
    }
}
