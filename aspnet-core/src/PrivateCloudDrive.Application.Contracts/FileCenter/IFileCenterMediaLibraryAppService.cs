using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 媒体库图片和视频查询应用服务契约。
/// </summary>
public interface IFileCenterMediaLibraryAppService : IApplicationService
{
    Task<PagedResultDto<FileNodeDto>> GetImagesAsync(GetMediaFilesInput input);

    Task<PagedResultDto<FileNodeDto>> GetVideosAsync(GetMediaFilesInput input);
}
