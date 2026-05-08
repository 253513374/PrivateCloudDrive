using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/tags")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Tags)]
public class FileCenterTagsController : PrivateCloudDriveController
{
    private readonly IFileCenterTagsAppService _tagsAppService;

    public FileCenterTagsController(IFileCenterTagsAppService tagsAppService)
    {
        _tagsAppService = tagsAppService;
    }

    [HttpGet]
    public virtual Task<IReadOnlyList<FileTagDto>> GetListAsync()
    {
        return _tagsAppService.GetListAsync();
    }

    [HttpPost]
    public virtual Task<FileTagDto> CreateAsync([FromBody] CreateFileTagInput input)
    {
        return _tagsAppService.CreateAsync(input);
    }

    [HttpPut("{id:guid}")]
    public virtual Task<FileTagDto> UpdateAsync(Guid id, [FromBody] UpdateFileTagInput input)
    {
        return _tagsAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id:guid}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _tagsAppService.DeleteAsync(id);
    }

    [HttpPost("/api/file-center/nodes/{nodeId:guid}/tags/{tagId:guid}")]
    public virtual Task AddToNodeAsync(Guid nodeId, Guid tagId)
    {
        return _tagsAppService.AddToNodeAsync(nodeId, tagId);
    }

    [HttpDelete("/api/file-center/nodes/{nodeId:guid}/tags/{tagId:guid}")]
    public virtual Task RemoveFromNodeAsync(Guid nodeId, Guid tagId)
    {
        return _tagsAppService.RemoveFromNodeAsync(nodeId, tagId);
    }

    [HttpPost("/api/file-center/nodes/{nodeId:guid}/favorite")]
    public virtual Task<FileNodeDto> SetFavoriteAsync(Guid nodeId, [FromBody] SetFileFavoriteInput input)
    {
        return _tagsAppService.SetFavoriteAsync(nodeId, input);
    }
}
