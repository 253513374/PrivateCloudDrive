using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 公开分享访问应用服务契约。
/// </summary>
public interface IFileCenterPublicSharesAppService : IApplicationService
{
    Task<PublicFileShareDto> GetAsync(string token);

    Task<PublicFileShareDto> VerifyPasswordAsync(string token, VerifySharePasswordInput input);

    Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password = null,
        CancellationToken cancellationToken = default);
}
