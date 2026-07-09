using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体文件的派生资产聚合，保存图片/视频尺寸、时长、缩略图和处理状态。
/// 原始文件仍由 FileNode/BlobObject 管理，本聚合只描述媒体库和预览所需的元数据。
/// </summary>
public class MediaAsset : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid FileNodeId { get; private set; }

    public MediaAssetMediaType MediaType { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public long? DurationMilliseconds { get; private set; }

    public string? Codec { get; private set; }

    public DateTime? TakenAt { get; private set; }

    public Guid? ThumbnailBlobObjectId { get; private set; }

    public Guid? PreviewBlobObjectId { get; private set; }

    public string? MetadataJson { get; private set; }

    public MediaAssetProcessStatus ProcessStatus { get; private set; }

    public string? ProcessError { get; private set; }

    protected MediaAsset()
    {
    }

    private MediaAsset(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid fileNodeId,
        MediaAssetMediaType mediaType)
        : base(id)
    {
        TenantId = tenantId;
        OwnerId = ownerId;
        FileNodeId = fileNodeId;
        MediaType = mediaType;
        ProcessStatus = MediaAssetProcessStatus.Pending;
    }

    /// <summary>
    /// 为图片或视频创建待处理媒体资产，等待后台任务生成缩略图和元数据。
    /// </summary>
    public static MediaAsset CreatePending(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid fileNodeId,
        MediaAssetMediaType mediaType)
    {
        return new MediaAsset(id, tenantId, ownerId, fileNodeId, mediaType);
    }

    /// <summary>
    /// 标记媒体资产开始处理，并清空上一次错误信息。
    /// 只允许从 Pending 或 Failed（重试）状态进入 Processing。
    /// </summary>
    public void MarkProcessing()
    {
        if (ProcessStatus is not (MediaAssetProcessStatus.Pending or MediaAssetProcessStatus.Failed))
        {
            throw new InvalidOperationException(
                $"Cannot mark processing from status {ProcessStatus}.");
        }

        ProcessStatus = MediaAssetProcessStatus.Processing;
        ProcessError = null;
    }

    /// <summary>
    /// 标记图片处理完成，记录尺寸、拍摄时间和缩略图 Blob。
    /// 只允许从 Processing 状态进入 Completed。
    /// </summary>
    public void MarkImageProcessed(
        int width,
        int height,
        DateTime? takenAt,
        Guid thumbnailBlobObjectId,
        string? metadataJson = null)
    {
        GuardOnProcessing(nameof(MarkImageProcessed));

        Width = width;
        Height = height;
        TakenAt = takenAt;
        ThumbnailBlobObjectId = thumbnailBlobObjectId;
        MetadataJson = metadataJson;
        ProcessStatus = MediaAssetProcessStatus.Completed;
        ProcessError = null;
    }

    /// <summary>
    /// 标记视频处理完成，记录尺寸、时长、编码信息和封面缩略图 Blob。
    /// 只允许从 Processing 状态进入 Completed。
    /// </summary>
    public void MarkVideoProcessed(
        int width,
        int height,
        long durationMilliseconds,
        string? codec,
        Guid thumbnailBlobObjectId,
        string? metadataJson = null)
    {
        GuardOnProcessing(nameof(MarkVideoProcessed));

        Width = width;
        Height = height;
        DurationMilliseconds = durationMilliseconds;
        Codec = string.IsNullOrWhiteSpace(codec)
            ? null
            : Check.Length(codec, nameof(codec), MediaAssetConsts.MaxCodecLength);
        ThumbnailBlobObjectId = thumbnailBlobObjectId;
        MetadataJson = metadataJson;
        ProcessStatus = MediaAssetProcessStatus.Completed;
        ProcessError = null;
    }

    /// <summary>
    /// 标记媒体处理失败，并截断保存已脱敏的可展示错误原因。
    /// 只允许从 Processing 状态进入 Failed。
    /// </summary>
    public void MarkFailed(string error)
    {
        if (ProcessStatus != MediaAssetProcessStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot mark failed from status {ProcessStatus}.");
        }

        ProcessStatus = MediaAssetProcessStatus.Failed;
        ProcessError = error?.Length > MediaAssetConsts.MaxProcessErrorLength
            ? error[..MediaAssetConsts.MaxProcessErrorLength]
            : error;
    }

    private void GuardOnProcessing(string methodName)
    {
        if (ProcessStatus != MediaAssetProcessStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot call {methodName} from status {ProcessStatus}.");
        }
    }
}
