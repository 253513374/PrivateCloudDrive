using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using PrivateCloudDrive.FileCenter;
using PrivateCloudDrive.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Linq;
using Volo.Abp.Settings;
using Volo.Abp.Users;

namespace PrivateCloudDrive.AdminIdentity;

/// <summary>
/// 管理员用户管理应用服务，复用 ABP IdentityUserAppService 实现核心操作。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
public class AdminIdentityUserAppService : PrivateCloudDriveAppService, IAdminIdentityUserAppService
{
    private const long DefaultUserStorageQuotaInBytes = 10737418240;

    private readonly IIdentityUserAppService _identityUserAppService;
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<BlobObject, Guid> _blobObjectRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ISettingProvider _settingProvider;
    private readonly IConfiguration _configuration;

    public AdminIdentityUserAppService(
        IIdentityUserAppService identityUserAppService,
        IdentityUserManager userManager,
        IRepository<BlobObject, Guid> blobObjectRepository,
        IAsyncQueryableExecuter asyncExecuter,
        ISettingProvider settingProvider,
        IConfiguration configuration)
    {
        _identityUserAppService = identityUserAppService;
        _userManager = userManager;
        _blobObjectRepository = blobObjectRepository;
        _asyncExecuter = asyncExecuter;
        _settingProvider = settingProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// 获取用户分页列表，返回用户基本信息和配额使用状态。
    /// 仅在当前租户范围内查询用户。
    /// </summary>
    public virtual async Task<PagedResultDto<AdminIdentityUserDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var identityInput = new GetIdentityUsersInput
        {
            MaxResultCount = input.MaxResultCount,
            SkipCount = input.SkipCount,
            Sorting = input.Sorting
        };

        var pagedResult = await _identityUserAppService.GetListAsync(identityInput);

        var items = pagedResult.Items
            .Where(user => CurrentTenant.Id == null || user.TenantId == CurrentTenant.Id)
            .Select(user => new AdminIdentityUserDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            UserName = user.UserName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            StorageQuotaBytes = DefaultUserStorageQuotaInBytes,
            StorageUsedBytes = 0,
            CreationTime = user.CreationTime,
            LastLoginTime = user.LastModificationTime
        }).ToList();

        return new PagedResultDto<AdminIdentityUserDto>(pagedResult.TotalCount, items);
    }

    /// <summary>
    /// 创建新用户，支持指定容量配额。
    /// </summary>
    public virtual async Task<AdminIdentityUserDto> CreateAsync(AdminCreateUserInput input)
    {
        var createInput = new IdentityUserCreateDto
        {
            UserName = input.UserName,
            Email = input.Email,
            Password = input.Password,
            IsActive = true
        };

        var createdUser = await _identityUserAppService.CreateAsync(createInput);

        return new AdminIdentityUserDto
        {
            Id = createdUser.Id,
            TenantId = createdUser.TenantId,
            UserName = createdUser.UserName,
            Email = createdUser.Email ?? string.Empty,
            IsActive = true,
            StorageQuotaBytes = input.StorageQuotaBytes ?? await GetUserQuotaSettingAsync(),
            StorageUsedBytes = 0,
            CreationTime = createdUser.CreationTime
        };
    }

    /// <summary>
    /// 禁用一个用户。管理员不能禁用自己。
    /// </summary>
    public virtual async Task DisableAsync(Guid userId)
    {
        if (CurrentUser.Id.HasValue && CurrentUser.Id.Value == userId)
        {
            throw new AbpAuthorizationException("Cannot disable yourself.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AbpAuthorizationException("User not found.");
        }

        user.SetIsActive(false);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new AbpAuthorizationException($"Failed to disable user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// 启用一个用户。
    /// </summary>
    public virtual async Task EnableAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AbpAuthorizationException("User not found.");
        }

        user.SetIsActive(true);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new AbpAuthorizationException($"Failed to enable user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// 重置用户密码（无需原密码）。
    /// </summary>
    public virtual async Task ResetPasswordAsync(Guid userId, AdminResetPasswordInput input)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AbpAuthorizationException("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, input.NewPassword);
        if (!result.Succeeded)
        {
            throw new AbpAuthorizationException($"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// 设置用户存储容量配额。
    /// </summary>
    public virtual async Task SetQuotaAsync(Guid userId, AdminSetQuotaInput input)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AbpAuthorizationException("User not found.");
        }

        user.ExtraProperties["StorageQuotaBytes"] = input.StorageQuotaBytes.ToString();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new AbpAuthorizationException($"Failed to set quota: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private async Task<long> GetUsedStorageSizeAsync(Guid ownerId)
    {
        var queryable = await _blobObjectRepository.GetQueryableAsync();
        var sizes = await _asyncExecuter.ToListAsync(
            queryable
                .Where(blob => blob.TenantId == CurrentTenant.Id && blob.OwnerId == ownerId)
                .Select(blob => blob.Size));

        return sizes.Sum();
    }

    private async Task<long> GetUserQuotaSettingAsync()
    {
        var value = await _settingProvider.GetOrNullAsync(
            PrivateCloudDrive.Settings.PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes);

        return long.TryParse(value, out var parsedValue) ? parsedValue : DefaultUserStorageQuotaInBytes;
    }
}
