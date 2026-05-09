using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Models.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 分片上传 HTTP API 控制器。
/// 负责接收客户端上传会话、分片文件和完成/取消指令，业务校验委托给应用服务。
/// </summary>
[Route("api/file-center/upload-sessions")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Upload)]
public class FileCenterUploadSessionsController : PrivateCloudDriveController
{
    private readonly IFileCenterChunkUploadService _chunkUploadService;

    /// <summary>
    /// 初始化 <see cref="FileCenterUploadSessionsController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterUploadSessionsController(IFileCenterChunkUploadService chunkUploadService)
    {
        _chunkUploadService = chunkUploadService;
    }

    /// <summary>
    /// 创建分片上传会话。
    /// </summary>
    [HttpPost]
    public virtual Task<UploadSessionDto> CreateAsync([FromBody] CreateUploadSessionInput input)
    {
        return _chunkUploadService.CreateAsync(input);
    }

    /// <summary>
    /// 查询指定资源或配置，并返回可被客户端消费的数据模型。
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<UploadSessionDto> GetAsync(Guid id)
    {
        return _chunkUploadService.GetAsync(id);
    }

    /// <summary>
    /// 上传单个分片文件。multipart 文件流只在当前请求内使用，不在控制器层落盘。
    /// </summary>
    [HttpPut("{id}/chunks/{chunkIndex:int}")]
    [Consumes("multipart/form-data")]
    public virtual async Task<UploadChunkResultDto> UploadChunkAsync(
        Guid id,
        int chunkIndex,
        [FromForm] UploadChunkForm input)
    {
        await using var stream = input.Chunk.OpenReadStream();

        return await _chunkUploadService.UploadChunkAsync(
            id,
            chunkIndex,
            stream,
            input.Chunk.Length,
            HttpContext.RequestAborted);
    }

    /// <summary>
    /// 完成分片上传并合并为最终文件。
    /// </summary>
    [HttpPost("{id}/complete")]
    public virtual Task<FileNodeDto> CompleteAsync(Guid id)
    {
        return _chunkUploadService.CompleteAsync(id, HttpContext.RequestAborted);
    }

    /// <summary>
    /// 执行Cancel操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    [HttpDelete("{id}")]
    public virtual Task CancelAsync(Guid id)
    {
        return _chunkUploadService.CancelAsync(id);
    }
}
