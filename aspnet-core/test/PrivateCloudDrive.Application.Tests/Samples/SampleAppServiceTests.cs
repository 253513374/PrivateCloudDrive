using Shouldly;
using System.Threading.Tasks;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace PrivateCloudDrive.Samples;

/* This is just an example test class.
 * Normally, you don't test code of the modules you are using
 * (like IIdentityUserAppService here).
 * Only test your own application services.
 */
/// <summary>
/// 表示SampleAppServiceTests组件，封装对应业务场景的状态或行为。
/// </summary>
public abstract class SampleAppServiceTests<TStartupModule> : PrivateCloudDriveApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IIdentityUserAppService _userAppService;

    protected SampleAppServiceTests()
    {
        _userAppService = GetRequiredService<IIdentityUserAppService>();
    }

    /// <summary>
    /// 执行Initial_Data_Should_Contain_Admin_User操作，封装该场景下的业务规则、异常处理和结果返回。
    /// </summary>
    [Fact]
    public async Task Initial_Data_Should_Contain_Admin_User()
    {
        //Act
        var result = await _userAppService.GetListAsync(new GetIdentityUsersInput());

        //Assert
        result.TotalCount.ShouldBeGreaterThan(0);
        result.Items.ShouldContain(u => u.UserName == "admin");
    }
}
