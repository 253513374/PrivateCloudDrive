using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体处理状态查询输入。
/// </summary>
public class GetMediaProcessingStatusInput : PagedResultRequestDto
{
    public MediaAssetProcessStatus? Status { get; set; }

    public MediaAssetMediaType? MediaType { get; set; }
}
