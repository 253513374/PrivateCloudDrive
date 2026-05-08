using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.OperationLogs;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.Controllers.OperationLogs;

[Route("api/operation-logs")]
[Authorize(PrivateCloudDrivePermissions.OperationLogs.View)]
public class OperationLogsController : PrivateCloudDriveController
{
    private readonly IOperationLogsAppService _operationLogsAppService;

    public OperationLogsController(IOperationLogsAppService operationLogsAppService)
    {
        _operationLogsAppService = operationLogsAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<OperationLogDto>> GetListAsync([FromQuery] GetOperationLogsInput input)
    {
        return _operationLogsAppService.GetListAsync(input);
    }
}
