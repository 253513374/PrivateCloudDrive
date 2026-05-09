using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.OperationLogs;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.OperationLogs;

/// <summary>
/// 提供OperationLogs相关 HTTP API 入口，负责请求绑定并委托应用服务处理业务逻辑。
/// </summary>
[Route("api/operation-logs")]
[Authorize(PrivateCloudDrivePermissions.OperationLogs.View)]
public class OperationLogsController : PrivateCloudDriveController
{
    private readonly IOperationLogsAppService _operationLogsAppService;

    /// <summary>
    /// 初始化 <see cref="OperationLogsController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public OperationLogsController(IOperationLogsAppService operationLogsAppService)
    {
        _operationLogsAppService = operationLogsAppService;
    }

    /// <summary>
    /// 查询分页列表数据，并按当前用户、租户和输入条件进行过滤。
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<OperationLogDto>> GetListAsync([FromQuery] GetOperationLogsInput input)
    {
        return _operationLogsAppService.GetListAsync(input);
    }
}
