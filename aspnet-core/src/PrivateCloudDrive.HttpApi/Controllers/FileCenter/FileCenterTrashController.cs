using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 回收站 HTTP API 控制器。
/// </summary>
[Route("api/file-center/trash")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterTrashController : PrivateCloudDriveController
{
    private readonly IFileCenterFoldersAppService _foldersAppService;
    private readonly IFileCenterTrashCleanupAppService _trashCleanupAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterTrashController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterTrashController(
        IFileCenterFoldersAppService foldersAppService,
        IFileCenterTrashCleanupAppService trashCleanupAppService)
    {
        _foldersAppService = foldersAppService;
        _trashCleanupAppService = trashCleanupAppService;
    }

    /// <summary>
    /// 分页查询当前用户回收站节点。
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<FileNodeDto>> GetListAsync([FromQuery] PagedResultRequestDto input)
    {
        return _foldersAppService.GetDeletedListAsync(input);
    }

    /// <summary>
    /// 清空当前用户回收站。
    /// </summary>
    [HttpDelete]
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
    public virtual Task EmptyAsync()
    {
        return _foldersAppService.EmptyTrashAsync();
    }

    /// <summary>
    /// 获取回收站清理建议，包含空间占用统计和提示文案。
    /// </summary>
    [HttpGet("cleanup-advice")]
    public virtual Task<TrashCleanupAdviceDto> GetCleanupAdviceAsync()
    {
        return _trashCleanupAppService.GetAdviceAsync();
    }
}
