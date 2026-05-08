using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class GetMediaFilesInput : PagedResultRequestDto
{
    public Guid? TagId { get; set; }

    public bool? IsFavorite { get; set; }
}
