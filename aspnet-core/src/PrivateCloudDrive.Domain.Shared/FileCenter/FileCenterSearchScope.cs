namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 文件中心搜索范围，用于区分仅搜索当前目录或搜索当前用户全部文件树。
/// </summary>
public enum FileCenterSearchScope
{
    /// <summary>
    /// 仅搜索当前 ParentId 指向的直接子节点。
    /// </summary>
    CurrentFolder = 0,

    /// <summary>
    /// 搜索当前用户在当前租户下的全部未删除节点。
    /// </summary>
    All = 1
}
