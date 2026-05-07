using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Models.FileCenter;
using PrivateCloudDrive.Permissions;

namespace PrivateCloudDrive.Controllers.FileCenter;

[Route("api/file-center/upload-sessions")]
[Authorize(PrivateCloudDrivePermissions.FileCenter.Upload)]
public class FileCenterUploadSessionsController : PrivateCloudDriveController
{
    private readonly IFileCenterChunkUploadService _chunkUploadService;

    public FileCenterUploadSessionsController(IFileCenterChunkUploadService chunkUploadService)
    {
        _chunkUploadService = chunkUploadService;
    }

    [HttpPost]
    public virtual Task<UploadSessionDto> CreateAsync(CreateUploadSessionInput input)
    {
        return _chunkUploadService.CreateAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<UploadSessionDto> GetAsync(Guid id)
    {
        return _chunkUploadService.GetAsync(id);
    }

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

    [HttpPost("{id}/complete")]
    public virtual Task<FileNodeDto> CompleteAsync(Guid id)
    {
        return _chunkUploadService.CompleteAsync(id, HttpContext.RequestAborted);
    }

    [HttpDelete("{id}")]
    public virtual Task CancelAsync(Guid id)
    {
        return _chunkUploadService.CancelAsync(id);
    }
}
