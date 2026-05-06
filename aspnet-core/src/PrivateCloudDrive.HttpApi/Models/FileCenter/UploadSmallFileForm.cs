using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrivateCloudDrive.Models.FileCenter;

public class UploadSmallFileForm
{
    public Guid? ParentId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
