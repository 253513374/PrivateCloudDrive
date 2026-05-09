using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.OperationLogs;

/// <summary>
/// 提供IOperationLogs相关应用服务编排，承接权限校验、业务规则调用与 DTO 映射。
/// </summary>
public interface IOperationLogsAppService : IApplicationService
{
    Task<PagedResultDto<OperationLogDto>> GetListAsync(GetOperationLogsInput input);
}
