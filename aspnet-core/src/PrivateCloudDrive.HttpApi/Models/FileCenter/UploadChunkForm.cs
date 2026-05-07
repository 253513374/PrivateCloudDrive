using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrivateCloudDrive.Models.FileCenter;

public class UploadChunkForm
{
    [Required]
    public IFormFile Chunk { get; set; } = null!;
}
