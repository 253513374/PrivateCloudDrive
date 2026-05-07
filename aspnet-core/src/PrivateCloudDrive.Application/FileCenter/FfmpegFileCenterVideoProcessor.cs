using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.FileCenter;

public class FfmpegFileCenterVideoProcessor : IFileCenterVideoProcessor, ITransientDependency
{
    private readonly IFileCenterBlobStoragePathProvider _storagePathProvider;
    private readonly FileCenterMediaProcessingOptions _options;

    public FfmpegFileCenterVideoProcessor(
        IFileCenterBlobStoragePathProvider storagePathProvider,
        IOptions<FileCenterMediaProcessingOptions> options)
    {
        _storagePathProvider = storagePathProvider;
        _options = options.Value;
    }

    public virtual async Task<FileCenterVideoProcessingResult> ProcessAsync(
        Stream videoStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var tempRootPath = _options.GetTempRootPath(_storagePathProvider.GetStorageRootPath());
        Directory.CreateDirectory(tempRootPath);

        var inputPath = Path.Combine(tempRootPath, $"{Guid.NewGuid():N}{GetSafeExtension(fileName)}");
        var thumbnailPath = Path.Combine(tempRootPath, $"{Guid.NewGuid():N}.jpg");

        try
        {
            await using (var inputFile = File.Create(inputPath))
            {
                if (videoStream.CanSeek)
                {
                    videoStream.Position = 0;
                }

                await videoStream.CopyToAsync(inputFile, cancellationToken);
            }

            var probeJson = await RunProcessAsync(
                _options.FfprobePath,
                new[]
                {
                    "-v",
                    "error",
                    "-print_format",
                    "json",
                    "-show_format",
                    "-show_streams",
                    inputPath
                },
                cancellationToken);

            var metadata = ParseProbeResult(probeJson);

            await RunProcessAsync(
                _options.FfmpegPath,
                new[]
                {
                    "-y",
                    "-i",
                    inputPath,
                    "-frames:v",
                    "1",
                    "-vf",
                    $"scale={_options.GetVideoThumbnailWidth()}:-2",
                    "-q:v",
                    "3",
                    thumbnailPath
                },
                cancellationToken);

            var thumbnailBytes = await File.ReadAllBytesAsync(thumbnailPath, cancellationToken);
            if (thumbnailBytes.Length == 0)
            {
                throw new InvalidOperationException("FFmpeg generated an empty video thumbnail.");
            }

            return new FileCenterVideoProcessingResult
            {
                Width = metadata.Width,
                Height = metadata.Height,
                DurationMilliseconds = metadata.DurationMilliseconds,
                Codec = metadata.Codec,
                ThumbnailBytes = thumbnailBytes,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    metadata.Width,
                    metadata.Height,
                    metadata.DurationMilliseconds,
                    metadata.Codec,
                    ThumbnailWidth = _options.GetVideoThumbnailWidth()
                })
            };
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(thumbnailPath);
        }
    }

    private static string GetSafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return string.IsNullOrWhiteSpace(extension) ||
               extension.Length > 16 ||
               extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            ? ".bin"
            : extension;
    }

    private static async Task<string> RunProcessAsync(
        string executablePath,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to start media processor '{executablePath}'. Ensure FFmpeg/FFprobe is installed or configure FileCenter:MediaProcessing paths.",
                exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Media processor '{executablePath}' failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }

    private static FileCenterVideoMetadata ParseProbeResult(string probeJson)
    {
        using var document = JsonDocument.Parse(probeJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("streams", out var streams) ||
            streams.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("FFprobe result does not contain stream metadata.");
        }

        JsonElement? videoStream = null;
        foreach (var stream in streams.EnumerateArray())
        {
            if (stream.TryGetProperty("codec_type", out var codecType) &&
                codecType.GetString() == "video")
            {
                videoStream = stream;
                break;
            }
        }

        if (videoStream == null)
        {
            throw new InvalidOperationException("FFprobe result does not contain a video stream.");
        }

        var width = GetRequiredInt(videoStream.Value, "width");
        var height = GetRequiredInt(videoStream.Value, "height");
        var codec = GetOptionalString(videoStream.Value, "codec_name");
        var durationMilliseconds = GetDurationMilliseconds(videoStream.Value, root);

        return new FileCenterVideoMetadata(width, height, durationMilliseconds, codec);
    }

    private static int GetRequiredInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : throw new InvalidOperationException($"FFprobe result does not contain '{propertyName}'.");
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }

    private static long GetDurationMilliseconds(JsonElement videoStream, JsonElement root)
    {
        var duration = GetOptionalDurationSeconds(videoStream);

        if (duration == null &&
            root.TryGetProperty("format", out var format))
        {
            duration = GetOptionalDurationSeconds(format);
        }

        if (duration == null)
        {
            throw new InvalidOperationException("FFprobe result does not contain video duration.");
        }

        return Convert.ToInt64(Math.Round(duration.Value * 1000, MidpointRounding.AwayFromZero));
    }

    private static double? GetOptionalDurationSeconds(JsonElement element)
    {
        if (!element.TryGetProperty("duration", out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String &&
               double.TryParse(
                   property.GetString(),
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out var parsed)
            ? parsed
            : null;
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temporary files are best-effort cleanup; processing status should reflect media errors only.
        }
    }

    private sealed record FileCenterVideoMetadata(
        int Width,
        int Height,
        long DurationMilliseconds,
        string? Codec);
}
