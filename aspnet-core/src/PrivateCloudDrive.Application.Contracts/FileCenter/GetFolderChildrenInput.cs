using System;
using Volo.Abp.Application.Dtos;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示GetFolderChildren请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class GetFolderChildrenInput : PagedResultRequestDto
{
    public Guid? ParentId { get; set; }

    public Guid? TagId { get; set; }

    public bool? IsFavorite { get; set; }
}
