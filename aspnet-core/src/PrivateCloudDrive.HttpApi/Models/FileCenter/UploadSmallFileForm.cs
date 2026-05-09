using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrivateCloudDrive.Models.FileCenter;

/// <summary>
/// 表示文件中心UploadSmallFileForm，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class UploadSmallFileForm
{
    public Guid? ParentId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
