using Microsoft.Extensions.Localization;
using PrivateCloudDrive.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace PrivateCloudDrive;

[Dependency(ReplaceServices = true)]
public class PrivateCloudDriveBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PrivateCloudDriveResource> _localizer;

    public PrivateCloudDriveBrandingProvider(IStringLocalizer<PrivateCloudDriveResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
