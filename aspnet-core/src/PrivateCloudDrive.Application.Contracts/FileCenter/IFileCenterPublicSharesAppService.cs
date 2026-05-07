using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterPublicSharesAppService : IApplicationService
{
    Task<PublicFileShareDto> GetAsync(string token);

    Task<PublicFileShareDto> VerifyPasswordAsync(string token, VerifySharePasswordInput input);

    Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password = null,
        CancellationToken cancellationToken = default);
}
