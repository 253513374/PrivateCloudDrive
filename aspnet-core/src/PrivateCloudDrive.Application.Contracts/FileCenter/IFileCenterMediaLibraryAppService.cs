using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterMediaLibraryAppService : IApplicationService
{
    Task<PagedResultDto<FileNodeDto>> GetImagesAsync(GetMediaFilesInput input);

    Task<PagedResultDto<FileNodeDto>> GetVideosAsync(GetMediaFilesInput input);
}
