using System;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示MoveFileNode请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class MoveFileNodeInput
{
    public Guid? ParentId { get; set; }
}
