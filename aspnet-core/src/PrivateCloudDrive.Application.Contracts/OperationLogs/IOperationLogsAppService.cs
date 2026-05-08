using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace PrivateCloudDrive.OperationLogs;

public interface IOperationLogsAppService : IApplicationService
{
    Task<PagedResultDto<OperationLogDto>> GetListAsync(GetOperationLogsInput input);
}
