using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrivateCloudDrive.Models.FileCenter;

/// <summary>
/// 表示文件中心UploadChunkForm，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class UploadChunkForm
{
    [Required]
    public IFormFile Chunk { get; set; } = null!;
}
