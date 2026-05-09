namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示SetFileFavorite请求输入参数，用于约束客户端提交的数据。
/// </summary>
public class SetFileFavoriteInput
{
    public bool IsFavorite { get; set; }
}
