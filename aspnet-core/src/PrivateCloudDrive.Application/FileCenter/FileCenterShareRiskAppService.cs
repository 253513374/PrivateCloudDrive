using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分享风险提示应用服务。
/// 聚合分享风险指标并返回可读文案，不暴露敏感数据。
/// 普通用户只能查看自己的风险；管理员可以查询任意用户。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Share)]
public class FileCenterShareRiskAppService : FileCenterAppService, IFileCenterShareRiskAppService
{
    private readonly IRepository<FileShare, Guid> _shareRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IClock _clock;

    /// <summary>
    /// 初始化 <see cref="FileCenterShareRiskAppService"/> 的新实例。
    /// </summary>
    public FileCenterShareRiskAppService(
        IRepository<FileShare, Guid> shareRepository,
        IAsyncQueryableExecuter asyncExecuter,
        IClock clock)
    {
        _shareRepository = shareRepository;
        _asyncExecuter = asyncExecuter;
        _clock = clock;
    }

    /// <summary>
    /// 获取当前用户的分享风险提示。
    /// </summary>
    public virtual async Task<ShareRiskDto> GetMyRiskAsync()
    {
        var ownerId = GetOwnerId();
        return await BuildRiskDtoAsync(ownerId);
    }

    /// <summary>
    /// 管理员查询指定用户的分享风险。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<ShareRiskDto> GetUserRiskAsync(Guid userId)
    {
        return await BuildRiskDtoAsync(userId);
    }

    private async Task<ShareRiskDto> BuildRiskDtoAsync(Guid ownerId)
    {
        var queryable = (await _shareRepository.GetQueryableAsync())
            .Where(share =>
                share.TenantId == CurrentTenant.Id &&
                share.OwnerId == ownerId &&
                share.IsEnabled);

        var allShares = await _asyncExecuter.ToListAsync(queryable);

        var now = _clock.Now;

        // 无过期时间：ExpirationTime 为 null
        var noExpirationShares = allShares
            .Where(s => s.ExpirationTime == null)
            .ToList();

        // 公开（无需密码）分享：RequiresPassword == false
        var publicShares = allShares
            .Where(s => !s.RequiresPassword)
            .ToList();

        // 长时间未使用的分享：VisitCount == 0
        var unusedShares = allShares
            .Where(s => s.VisitCount == 0)
            .ToList();

        // 已过期分享（ExpirationTime < now）
        var expiredShares = allShares
            .Where(s => s.ExpirationTime.HasValue && s.ExpirationTime.Value <= now)
            .ToList();

        return new ShareRiskDto
        {
            UserId = ownerId,
            TotalShares = allShares.Count,
            NoExpirationCount = noExpirationShares.Count,
            PublicNoPasswordCount = publicShares.Count,
            LongUnusedCount = unusedShares.Count,
            NoExpirationMessage = BuildNoExpirationMessage(noExpirationShares.Count),
            PublicShareMessage = BuildPublicShareMessage(publicShares.Count),
            UnusedShareMessage = BuildUnusedShareMessage(unusedShares.Count)
        };
    }

    private static string BuildNoExpirationMessage(int count)
    {
        if (count == 0)
            return "所有分享均设置了过期时间，没有安全风险。";

        return count switch
        {
            1 => "你有 1 个分享未设置过期时间，建议为包含重要文件的分享设置过期时间。",
            _ => $"你有 {count} 个分享未设置过期时间。设置过期时间可以降低长期暴露风险，建议定期检查。"
        };
    }

    private static string BuildPublicShareMessage(int count)
    {
        if (count == 0)
            return "所有分享均设置了密码保护，访问安全可控。";

        return count switch
        {
            1 => "你有 1 个公开分享（无需密码）。公开分享可能被搜索引擎索引，建议为敏感文件设置密码。",
            _ => $"你有 {count} 个公开分享（无需密码）。公开分享可能被搜索引擎索引，请确保不包含隐私信息。"
        };
    }

    private static string BuildUnusedShareMessage(int count)
    {
        if (count == 0)
            return "所有分享近期均被访问过。";

        return count switch
        {
            1 => "你有 1 个分享创建后从未被访问，可能已失效，建议确认后删除。",
            _ => $"你有 {count} 个分享创建后从未被访问，可能已失效，建议定期清理。"
        };
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }
}
