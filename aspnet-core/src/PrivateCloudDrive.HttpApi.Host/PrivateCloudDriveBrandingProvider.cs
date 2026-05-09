using Microsoft.Extensions.Localization;
using PrivateCloudDrive.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace PrivateCloudDrive;

/// <summary>
/// 表示PrivateCloudDriveBrandingProvider组件，封装对应业务场景的状态或行为。
/// </summary>
[Dependency(ReplaceServices = true)]
public class PrivateCloudDriveBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PrivateCloudDriveResource> _localizer;

    /// <summary>
    /// 初始化 <see cref="PrivateCloudDriveBrandingProvider"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public PrivateCloudDriveBrandingProvider(IStringLocalizer<PrivateCloudDriveResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
