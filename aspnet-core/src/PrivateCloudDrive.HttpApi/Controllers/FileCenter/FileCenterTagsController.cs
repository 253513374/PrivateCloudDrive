using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

/// <summary>
/// 文件标签和收藏 HTTP API 控制器。
/// </summary>
[Route("api/file-center/tags")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Tags)]
public class FileCenterTagsController : PrivateCloudDriveController
{
    private readonly IFileCenterTagsAppService _tagsAppService;

    /// <summary>
    /// 初始化 <see cref="FileCenterTagsController"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterTagsController(IFileCenterTagsAppService tagsAppService)
    {
        _tagsAppService = tagsAppService;
    }

    /// <summary>
    /// 查询分页列表数据，并按当前用户、租户和输入条件进行过滤。
    /// </summary>
    [HttpGet]
    public virtual Task<IReadOnlyList<FileTagDto>> GetListAsync()
    {
        return _tagsAppService.GetListAsync();
    }

    /// <summary>
    /// 创建新的业务资源，并在持久化前执行必要的权限和规则校验。
    /// </summary>
    [HttpPost]
    public virtual Task<FileTagDto> CreateAsync([FromBody] CreateFileTagInput input)
    {
        return _tagsAppService.CreateAsync(input);
    }

    /// <summary>
    /// 更新现有业务资源，并保持跨层数据和领域状态一致。
    /// </summary>
    [HttpPut("{id:guid}")]
    public virtual Task<FileTagDto> UpdateAsync(Guid id, [FromBody] UpdateFileTagInput input)
    {
        return _tagsAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _tagsAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 给指定文件节点添加标签。
    /// </summary>
    [HttpPost("/api/file-center/nodes/{nodeId:guid}/tags/{tagId:guid}")]
    public virtual Task AddToNodeAsync(Guid nodeId, Guid tagId)
    {
        return _tagsAppService.AddToNodeAsync(nodeId, tagId);
    }

    /// <summary>
    /// 删除指定业务资源；涉及文件中心时优先遵循回收站或安全删除语义。
    /// </summary>
    [HttpDelete("/api/file-center/nodes/{nodeId:guid}/tags/{tagId:guid}")]
    public virtual Task RemoveFromNodeAsync(Guid nodeId, Guid tagId)
    {
        return _tagsAppService.RemoveFromNodeAsync(nodeId, tagId);
    }

    /// <summary>
    /// 设置文件节点收藏状态。
    /// </summary>
    [HttpPost("/api/file-center/nodes/{nodeId:guid}/favorite")]
    public virtual Task<FileNodeDto> SetFavoriteAsync(Guid nodeId, [FromBody] SetFileFavoriteInput input)
    {
        return _tagsAppService.SetFavoriteAsync(nodeId, input);
    }
}
