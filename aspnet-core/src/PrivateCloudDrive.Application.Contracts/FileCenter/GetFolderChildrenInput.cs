using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

public class GetFolderChildrenInput : PagedResultRequestDto
{
    public Guid? ParentId { get; set; }
}
