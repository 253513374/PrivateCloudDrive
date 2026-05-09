using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrivateCloudDrive.FileCenter;

internal static class FileCenterMediaLibraryHelpers
{
    private static readonly string[] ImageExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif"
    };

    private static readonly string[] VideoExtensions =
    {
        ".mp4", ".m4v", ".mov", ".mkv", ".webm", ".avi"
    };

    public static MediaAssetMediaType? DetectMediaType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaAssetMediaType.Image;
            }

            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return MediaAssetMediaType.Video;
            }
        }

        var extension = Path.GetExtension(fileName);
        if (ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return MediaAssetMediaType.Image;
        }

        if (VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return MediaAssetMediaType.Video;
        }

        return null;
    }

    public static bool IsMediaNode(FileNode node, MediaAssetMediaType? mediaType = null)
    {
        var detectedMediaType = DetectMediaType(node.Name, node.ContentType);
        return detectedMediaType.HasValue && (!mediaType.HasValue || detectedMediaType.Value == mediaType.Value);
    }

    public static MediaTimelineItemDto ToTimelineItem(FileNode node, MediaAsset? asset)
    {
        var mediaType = asset?.MediaType ?? DetectMediaType(node.Name, node.ContentType)!.Value;
        var processStatus = asset?.ProcessStatus ?? MediaAssetProcessStatus.Pending;

        return new MediaTimelineItemDto
        {
            Id = node.Id,
            FileNodeId = node.Id,
            MediaAssetId = asset?.Id,
            Name = node.Name,
            MediaType = mediaType,
            Size = node.Size,
            ContentType = node.ContentType,
            TimelineTime = asset?.TakenAt ?? node.CreationTime,
            CreationTime = node.CreationTime,
            ThumbnailBlobObjectId = asset?.ThumbnailBlobObjectId,
            ProcessStatus = processStatus,
            ProcessErrorSummary = SanitizeProcessError(asset?.ProcessError),
            Width = asset?.Width,
            Height = asset?.Height,
            DurationMilliseconds = asset?.DurationMilliseconds,
            IsFavorite = node.IsFavorite
        };
    }

    public static MediaDetailDto ToDetail(FileNode node, MediaAsset? asset)
    {
        var mediaType = asset?.MediaType ?? DetectMediaType(node.Name, node.ContentType)!.Value;
        var processStatus = asset?.ProcessStatus ?? MediaAssetProcessStatus.Pending;

        return new MediaDetailDto
        {
            FileNodeId = node.Id,
            MediaAssetId = asset?.Id,
            Name = node.Name,
            MediaType = mediaType,
            Size = node.Size,
            ContentType = node.ContentType,
            Width = asset?.Width,
            Height = asset?.Height,
            DurationMilliseconds = asset?.DurationMilliseconds,
            Codec = asset?.Codec,
            TakenAt = asset?.TakenAt,
            ThumbnailBlobObjectId = asset?.ThumbnailBlobObjectId,
            PreviewBlobObjectId = asset?.PreviewBlobObjectId,
            ProcessStatus = processStatus,
            ProcessErrorSummary = SanitizeProcessError(asset?.ProcessError),
            CanPreview = mediaType == MediaAssetMediaType.Image
                ? processStatus != MediaAssetProcessStatus.Failed
                : processStatus == MediaAssetProcessStatus.Completed,
            CanRetryProcessing = processStatus is MediaAssetProcessStatus.Pending or MediaAssetProcessStatus.Failed
        };
    }

    public static string? SanitizeProcessError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var sanitized = error.Trim();
        sanitized = Regex.Replace(sanitized, @"[A-Za-z]:\\[^\r\n\t ]+", "[path]");
        sanitized = Regex.Replace(sanitized, @"(?<!:)//?[^\r\n\t ]*/[^\r\n\t ]+", "[path]");
        sanitized = Regex.Replace(
            sanitized,
            @"(?i)(token|secret|password|connectionstring|connection string)\s*[:=]\s*[^;\s]+",
            "$1=[redacted]");

        if (sanitized.Length > 180)
        {
            sanitized = sanitized[..180] + "...";
        }

        return sanitized;
    }
}
