namespace PrivateCloudDrive.MultiTenancy;

/// <summary>
/// 定义MultiTenancy相关常量，避免业务规则和协议值在代码中重复散落。
/// </summary>
public static class MultiTenancyConsts
{
    /* Enable/disable multi-tenancy easily in a single point.
     * If you will never need to multi-tenancy, you can remove
     * related modules and code parts, including this file.
     */
    public const bool IsEnabled = true;
}
