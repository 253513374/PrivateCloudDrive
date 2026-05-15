using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrivateCloudDrive.FileCenter;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 公开分享 HTTP API 控制器。
/// 该控制器允许匿名访问，必须依赖 token、过期时间、密码和下载权限共同约束安全边界。
/// </summary>
[AllowAnonymous]
[Route("api/public/shares")]
public class PublicFileSharesController : PrivateCloudDriveController
{
    private readonly IFileCenterPublicSharesAppService _publicSharesAppService;

    /// <summary>
    /// 初始化 <see cref="PublicFileSharesController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public PublicFileSharesController(IFileCenterPublicSharesAppService publicSharesAppService)
    {
        _publicSharesAppService = publicSharesAppService;
    }

    /// <summary>
    /// 查询公开分享信息；受密码保护的分享不会直接暴露文件内容。
    /// </summary>
    [HttpGet("{token}")]
    public virtual Task<PublicFileShareDto> GetAsync(string token)
    {
        return _publicSharesAppService.GetAsync(token);
    }

    /// <summary>
    /// 校验公开分享密码。明文密码仅通过请求体传入并交由应用服务即时比对；失败尝试受限速策略保护。
    /// </summary>
    [HttpPost("{token}/verify-password")]
    [EnableRateLimiting("PublicSharePassword")]
    public virtual Task<PublicFileShareDto> VerifyPasswordAsync(
        string token,
        VerifySharePasswordInput input)
    {
        return _publicSharesAppService.VerifyPasswordAsync(token, input);
    }

    /// <summary>
    /// 下载公开分享文件。应用服务会校验 token、密码需求、过期时间和 AllowDownload。
    /// 受密码保护的分享必须通过 X-Share-Password 请求头传入密码，避免密码进入 URL、代理日志或浏览器历史。
    /// </summary>
    [HttpGet("{token}/download")]
    [EnableRateLimiting("PublicSharePassword")]
    public virtual async Task<IActionResult> DownloadAsync(
        string token,
        [FromHeader(Name = "X-Share-Password")] string? password = null)
    {
        if (!FileCenterFileResultHelper.TryCreateRangeRequest(
                Request,
                out var range,
                out var errorResult))
        {
            return errorResult!;
        }

        try
        {
            var file = await _publicSharesAppService.GetDownloadAsync(
                token,
                password,
                range,
                HttpContext.RequestAborted);

            return FileCenterFileResultHelper.CreateFileResult(HttpContext, file, asAttachment: true);
        }
        catch (FileDownloadRangeNotSatisfiableException exception)
        {
            return FileCenterFileResultHelper.CreateRangeNotSatisfiableResult(HttpContext, exception);
        }
    }
}
