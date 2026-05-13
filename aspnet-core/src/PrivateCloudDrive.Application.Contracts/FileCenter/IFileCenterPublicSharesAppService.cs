using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 公开分享访问应用服务契约。
/// </summary>
public interface IFileCenterPublicSharesAppService : IApplicationService
{
    /// <summary>
    /// 根据公开分享 token 获取匿名可见的分享元数据。
    /// </summary>
    Task<PublicFileShareDto> GetAsync(string token);

    /// <summary>
    /// 校验公开分享密码并返回可访问的分享元数据。
    /// </summary>
    Task<PublicFileShareDto> VerifyPasswordAsync(string token, VerifySharePasswordInput input);

    /// <summary>
    /// 获取公开分享文件的完整下载流和响应元数据。
    /// </summary>
    Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按可选 Range 获取公开分享文件的下载流和响应元数据。
    /// </summary>
    Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password,
        FileDownloadRangeRequest? range,
        CancellationToken cancellationToken = default);
}
