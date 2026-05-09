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
    /// </summary>
    public void MarkProcessing()
    {
        ProcessStatus = MediaAssetProcessStatus.Processing;
        ProcessError = null;
    }

    /// <summary>
    /// 标记图片处理完成，记录尺寸、拍摄时间和缩略图 Blob。
    /// </summary>
    public void MarkImageProcessed(
        int width,
        int height,
        DateTime? takenAt,
        Guid thumbnailBlobObjectId,
        string? metadataJson = null)
    {
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
    /// </summary>
    public void MarkVideoProcessed(
        int width,
        int height,
        long durationMilliseconds,
        string? codec,
        Guid thumbnailBlobObjectId,
        string? metadataJson = null)
    {
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
    /// 标记媒体处理失败，并截断保存可展示的错误原因。
    /// </summary>
    public void MarkFailed(string error)
    {
        ProcessStatus = MediaAssetProcessStatus.Failed;
        ProcessError = Check.Length(error, nameof(error), MediaAssetConsts.MaxProcessErrorLength);
    }
}
