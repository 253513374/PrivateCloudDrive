using System;
using System.Collections.Generic;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心批量节点操作输入。
/// </summary>
public class BatchFileNodeInput
{
    public List<Guid> Ids { get; set; } = [];
}

/// <summary>
/// 文件中心批量移动输入。
/// </summary>
public class BatchMoveFileNodesInput : BatchFileNodeInput
{
    public Guid? ParentId { get; set; }
}

/// <summary>
/// 文件中心批量收藏输入。
/// </summary>
public class BatchSetFavoriteInput : BatchFileNodeInput
{
    public bool IsFavorite { get; set; }
}
