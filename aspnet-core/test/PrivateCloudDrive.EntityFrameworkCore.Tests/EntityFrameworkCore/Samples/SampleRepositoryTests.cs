using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.Samples;

/* This is just an example test class.
 * Normally, you don't test ABP framework code
 * (like default AppUser repository IRepository<AppUser, Guid> here).
 * Only test your custom repository methods.
 */
/// <summary>
/// 表示SampleRepositoryTests组件，封装对应业务场景的状态或行为。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class SampleRepositoryTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly IRepository<IdentityUser, Guid> _appUserRepository;

    /// <summary>
    /// 初始化 <see cref="SampleRepositoryTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public SampleRepositoryTests()
    {
        _appUserRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Query_AppUser()
    {
        /* Need to manually start Unit Of Work because
         * FirstOrDefaultAsync should be executed while db connection / context is available.
         */
        await WithUnitOfWorkAsync(async () =>
        {
                //Act
                var adminUser = await (await _appUserRepository.GetQueryableAsync())
                .Where(u => u.UserName == "admin")
                .FirstOrDefaultAsync();

                //Assert
                adminUser.ShouldNotBeNull();
        });
    }
}
