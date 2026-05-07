using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class GetFolderChildrenInput : PagedResultRequestDto
{
    public Guid? ParentId { get; set; }

    public Guid? TagId { get; set; }

    public bool? IsFavorite { get; set; }
}
