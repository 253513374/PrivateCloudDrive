using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 分享风险提示和回收站清理建议 API 集成测试。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterShareRiskAndTrashCleanupTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileUploadService _fileUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterSharesAppService _sharesAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterShareRiskAppService _shareRiskAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService _foldersAppService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterTrashCleanupAppService _trashCleanupAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化测试类，注入所需服务。
    /// </summary>
    public EfCoreFileCenterShareRiskAndTrashCleanupTests()
    {
        _fileUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileUploadService>();
        _sharesAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterSharesAppService>();
        _shareRiskAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterShareRiskAppService>();
        _foldersAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFoldersAppService>();
        _trashCleanupAppService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterTrashCleanupAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    // ========================================
    // P1-05: Share Risk Warning
    // ========================================

    /// <summary>
    /// P1-05-AC1：分享风险 API 报告无过期时间的分享数量。
    /// </summary>
    [Fact]
    public async Task Should_Report_No_Expiration_Shares()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("no-expiration.txt", Encoding.UTF8.GetBytes("test"));
            await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    ExpirationTime = null
                });

            var risk = await _shareRiskAppService.GetMyRiskAsync();

            risk.NoExpirationCount.ShouldBeGreaterThanOrEqualTo(1);
            risk.NoExpirationMessage.ShouldNotBeNullOrWhiteSpace();
            risk.TotalShares.ShouldBeGreaterThanOrEqualTo(1);
        });
    }

    /// <summary>
    /// P1-05-AC2：分享风险 API 报告公开（无需密码）分享数量。
    /// </summary>
    [Fact]
    public async Task Should_Report_Public_Shares()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("public-share.txt", Encoding.UTF8.GetBytes("test"));
            await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    Password = null
                });

            var risk = await _shareRiskAppService.GetMyRiskAsync();

            risk.PublicNoPasswordCount.ShouldBeGreaterThanOrEqualTo(1);
            risk.PublicShareMessage.ShouldNotBeNullOrWhiteSpace();
        });
    }

    /// <summary>
    /// P1-05-AC3：分享风险 API 报告长时间未使用的分享数量。
    /// </summary>
    [Fact]
    public async Task Should_Report_Unused_Shares()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("unused-share.txt", Encoding.UTF8.GetBytes("test"));
            await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id
                });

            var risk = await _shareRiskAppService.GetMyRiskAsync();

            risk.LongUnusedCount.ShouldBeGreaterThanOrEqualTo(1);
            risk.UnusedShareMessage.ShouldNotBeNullOrWhiteSpace();
        });
    }

    /// <summary>
    /// P1-05-AC4：风险文案不包含敏感数据。
    /// </summary>
    [Fact]
    public async Task Risk_Messages_Should_Not_Contain_Sensitive_Data()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("sensitive-check.txt", Encoding.UTF8.GetBytes("test"));
            var share = await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id
                });

            var risk = await _shareRiskAppService.GetMyRiskAsync();

            risk.NoExpirationMessage.ShouldNotContain(share.Token);
            risk.PublicShareMessage.ShouldNotContain(share.Token);
            risk.UnusedShareMessage.ShouldNotContain(share.Token);
            risk.NoExpirationMessage.ShouldNotContain("sensitive-check");
            risk.PublicShareMessage.ShouldNotContain("sensitive-check");
        });
    }

    /// <summary>
    /// P1-05 管理员可查询指定用户的分享风险。
    /// </summary>
    [Fact]
    public async Task Admin_Should_Query_User_Share_Risk()
    {
        var targetUserId = Guid.NewGuid();

        await WithCurrentUserAsync(targetUserId, async () =>
        {
            var fileNode = await UploadTextFileAsync("target-share.txt", Encoding.UTF8.GetBytes("test"));
            await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    ExpirationTime = null
                });
        });

        await WithCurrentUserAsync(Guid.NewGuid(), async () =>
        {
            var risk = await _shareRiskAppService.GetUserRiskAsync(targetUserId);
            risk.UserId.ShouldBe(targetUserId);
            risk.TotalShares.ShouldBeGreaterThanOrEqualTo(1);
            risk.NoExpirationCount.ShouldBeGreaterThanOrEqualTo(1);
            risk.NoExpirationMessage.ShouldNotBeNullOrWhiteSpace();
        });
    }

    /// <summary>
    /// P1-05：不同用户的数据隔离。
    /// </summary>
    [Fact]
    public async Task Share_Risk_Should_Be_Isolated_Per_User()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            var fileNode = await UploadTextFileAsync("first-user.txt", Encoding.UTF8.GetBytes("test"));
            await _sharesAppService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateFileShareInput
                {
                    FileNodeId = fileNode.Id,
                    ExpirationTime = null
                });
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            var risk = await _shareRiskAppService.GetMyRiskAsync();
            risk.TotalShares.ShouldBe(0);
            risk.NoExpirationCount.ShouldBe(0);
        });
    }

    // ========================================
    // P1-06: Trash Cleanup Advice
    // ========================================
    /// <summary>
    /// P1-06-AC1：回收站清理建议展示空间占用（字节而非文件数）。
    /// </summary>
    [Fact]
    public async Task Should_Report_Trash_Size_In_Bytes()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("trash-size-test.txt", Encoding.UTF8.GetBytes("some trash content"));
            await _fileUploadService.DeleteAsync(fileNode.Id);

            var advice = await _trashCleanupAppService.GetAdviceAsync();

            advice.TrashSizeBytes.ShouldBeGreaterThan(0);
            advice.TrashFileCount.ShouldBeGreaterThanOrEqualTo(1);
        });
    }

    /// <summary>
    /// P1-06-AC2：回收站清理建议包含保留天数信息。
    /// </summary>
    [Fact]
    public async Task Should_Report_Retention_Days()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var advice = await _trashCleanupAppService.GetAdviceAsync();

            // 即使回收站为空，也应返回有效的保留天数
            advice.RetentionDays.ShouldBeGreaterThan(0);
            advice.CleanupAdviceMessage.ShouldNotBeNullOrWhiteSpace();
        });
    }

    /// <summary>
    /// P1-06-AC3：文案实用、不制造紧迫感。
    /// </summary>
    [Fact]
    public async Task Cleanup_Advice_Message_Should_Be_Practical()
    {
        var userId = Guid.NewGuid();

        await WithCurrentUserAsync(userId, async () =>
        {
            var fileNode = await UploadTextFileAsync("advice-msg-test.txt", Encoding.UTF8.GetBytes("content"));
            await _fileUploadService.DeleteAsync(fileNode.Id);

            var advice = await _trashCleanupAppService.GetAdviceAsync();

            advice.CleanupAdviceMessage.ShouldNotBeNullOrWhiteSpace();
            advice.CleanupAdviceMessage.ShouldNotContain("紧急");
            advice.CleanupAdviceMessage.ShouldNotContain("立即");
        });
    }

    /// <summary>
    /// P1-06：不同用户的回收站数据隔离。
    /// </summary>
    [Fact]
    public async Task Trash_Stats_Should_Be_Isolated_Per_User()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        await WithCurrentUserAsync(firstUserId, async () =>
        {
            var fileNode = await UploadTextFileAsync("first-trash.txt", Encoding.UTF8.GetBytes("data"));
            await _fileUploadService.DeleteAsync(fileNode.Id);
        });

        await WithCurrentUserAsync(secondUserId, async () =>
        {
            var advice = await _trashCleanupAppService.GetAdviceAsync();
            advice.TrashSizeBytes.ShouldBe(0);
            advice.TrashFileCount.ShouldBe(0);
        });
    }

    // ========================================
    // Helpers
    // ========================================

    private async Task<PrivateCloudDrive.FileCenter.FileNodeDto> UploadTextFileAsync(string fileName, byte[] content)
    {
        await using var stream = new MemoryStream(content);
        return await _fileUploadService.UploadSmallFileAsync(
            parentId: null,
            fileName,
            "text/plain",
            stream,
            content.Length);
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "share-risk-trash-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }
}
