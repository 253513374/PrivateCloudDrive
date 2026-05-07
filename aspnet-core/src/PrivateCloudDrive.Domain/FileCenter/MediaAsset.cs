using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace PrivateCloudDrive.FileCenter;

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

    public static MediaAsset CreatePending(
        Guid id,
        Guid? tenantId,
        Guid ownerId,
        Guid fileNodeId,
        MediaAssetMediaType mediaType)
    {
        return new MediaAsset(id, tenantId, ownerId, fileNodeId, mediaType);
    }

    public void MarkProcessing()
    {
        ProcessStatus = MediaAssetProcessStatus.Processing;
        ProcessError = null;
    }

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

    public void MarkFailed(string error)
    {
        ProcessStatus = MediaAssetProcessStatus.Failed;
        ProcessError = Check.Length(error, nameof(error), MediaAssetConsts.MaxProcessErrorLength);
    }
}
