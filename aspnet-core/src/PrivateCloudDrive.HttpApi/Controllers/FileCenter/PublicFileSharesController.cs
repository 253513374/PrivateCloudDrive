using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;

namespace PrivateCloudDrive.Controllers.FileCenter;

[AllowAnonymous]
[Route("api/public/shares")]
public class PublicFileSharesController : PrivateCloudDriveController
{
    private readonly IFileCenterPublicSharesAppService _publicSharesAppService;

    public PublicFileSharesController(IFileCenterPublicSharesAppService publicSharesAppService)
    {
        _publicSharesAppService = publicSharesAppService;
    }

    [HttpGet("{token}")]
    public virtual Task<PublicFileShareDto> GetAsync(string token)
    {
        return _publicSharesAppService.GetAsync(token);
    }

    [HttpPost("{token}/verify-password")]
    public virtual Task<PublicFileShareDto> VerifyPasswordAsync(
        string token,
        VerifySharePasswordInput input)
    {
        return _publicSharesAppService.VerifyPasswordAsync(token, input);
    }

    [HttpGet("{token}/download")]
    public virtual async Task<IActionResult> DownloadAsync(
        string token,
        [FromQuery] string? password = null)
    {
        var file = await _publicSharesAppService.GetDownloadAsync(
            token,
            password,
            HttpContext.RequestAborted);

        return new FileStreamResult(file.Content, file.ContentType)
        {
            EnableRangeProcessing = true,
            FileDownloadName = file.FileName
        };
    }
}
