using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Linq;

namespace PrivateCloudDrive.MobileAuth;

[ExposeServices(
    typeof(IWechatAuthAppService),
    typeof(IWechatLoginService),
    typeof(WechatAuthAppService))]
public class WechatAuthAppService :
    PrivateCloudDriveAppService,
    IWechatAuthAppService,
    IWechatLoginService
{
    private readonly WechatLoginOptions _options;
    private readonly IWechatIdentityService _wechatIdentityService;
    private readonly IWechatBindingTicketStore _ticketStore;
    private readonly IWechatAuthRateLimiter _rateLimiter;
    private readonly IRepository<WechatUserBinding, Guid> _bindingRepository;
    private readonly IRepository<MobileAuthAuditLog, Guid> _auditLogRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public WechatAuthAppService(
        IOptions<WechatLoginOptions> options,
        IWechatIdentityService wechatIdentityService,
        IWechatBindingTicketStore ticketStore,
        IWechatAuthRateLimiter rateLimiter,
        IRepository<WechatUserBinding, Guid> bindingRepository,
        IRepository<MobileAuthAuditLog, Guid> auditLogRepository,
        IdentityUserManager userManager,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _options = options.Value;
        _wechatIdentityService = wechatIdentityService;
        _ticketStore = ticketStore;
        _rateLimiter = rateLimiter;
        _bindingRepository = bindingRepository;
        _auditLogRepository = auditLogRepository;
        _userManager = userManager;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
    }

    [AllowAnonymous]
    public virtual Task<WechatLoginSettingsDto> GetSettingsAsync()
    {
        return Task.FromResult(new WechatLoginSettingsDto
        {
            IsEnabled = _options.IsUsable(),
            AppId = _options.IsUsable() ? _options.AppId : null,
            Scope = _options.Scope,
            CallbackScheme = _options.CallbackScheme,
            AndroidPackageName = _options.Android.PackageName,
            IosBundleId = _options.iOS.BundleId,
            IosUrlScheme = _options.iOS.UrlScheme
        });
    }

    [Authorize]
    public virtual async Task<WechatBindingDto?> GetBindingAsync()
    {
        if (!CurrentUser.Id.HasValue || string.IsNullOrWhiteSpace(_options.AppId))
        {
            return null;
        }

        var binding = await FindEnabledBindingByUserAsync(CurrentUser.Id.Value);
        return binding == null ? null : ToDto(binding);
    }

    [Authorize]
    public virtual async Task<WechatBindingDto> BindCurrentAsync(BindCurrentWechatInput input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for WeChat binding.");
        }

        try
        {
            await _rateLimiter.CheckAsync(
                "bind-current",
                BuildCurrentUserRateLimitSubject(CurrentUser.Id.Value, input.DeviceIdHash));

            var identity = await ExchangeIdentityAsync(input.Code, input.Platform);
            var binding = await BindIdentityToUserAsync(identity, CurrentUser.Id.Value);

            await RecordAuditAsync(
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Success,
                failureReason: null,
                clientId: null,
                deviceIdHash: input.DeviceIdHash);

            return ToDto(binding);
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToWechatError(exception),
                clientId: null,
                deviceIdHash: input.DeviceIdHash);
            throw;
        }
    }

    [AllowAnonymous]
    public virtual async Task<WechatBindingDto> BindExistingAsync(BindExistingWechatInput input)
    {
        try
        {
            await _rateLimiter.CheckAsync(
                "bind-existing",
                BuildBindExistingRateLimitSubject(input.UserNameOrEmail));
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                userId: null,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToWechatError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }

        var identity = await _ticketStore.GetAsync(input.BindingTicket);
        if (identity == null)
        {
            await RecordAuditAsync(
                userId: null,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: WechatLoginConsts.BindingTicketNotFoundError,
                clientId: null,
                deviceIdHash: null);

            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatBindingTicketNotFound)
                .WithData("error", WechatLoginConsts.BindingTicketNotFoundError);
        }

        var user = await FindUserByNameOrEmailAsync(input.UserNameOrEmail);
        if (user == null || !user.IsActive)
        {
            await RecordAuditAsync(
                userId: user?.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "invalid_user_credentials",
                clientId: null,
                deviceIdHash: null);

            throw new AbpAuthorizationException("Invalid username or password.");
        }

        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            await RecordAuditAsync(
                userId: user.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "user_locked_out",
                clientId: null,
                deviceIdHash: null);

            throw new AbpAuthorizationException("Invalid username or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, input.Password))
        {
            var failureReason = "invalid_user_credentials";
            if (_userManager.SupportsUserLockout)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    failureReason = "user_locked_out";
                }
            }

            await RecordAuditAsync(
                userId: user.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: failureReason,
                clientId: null,
                deviceIdHash: null);

            throw new AbpAuthorizationException("Invalid username or password.");
        }

        if (_userManager.SupportsUserLockout)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        try
        {
            var binding = await BindIdentityToUserAsync(identity, user.Id);
            await _ticketStore.RemoveAsync(input.BindingTicket);

            await RecordAuditAsync(
                userId: user.Id,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Success,
                failureReason: null,
                clientId: null,
                deviceIdHash: null);

            return ToDto(binding);
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                userId: user.Id,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.WeChatBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToWechatError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }
    }

    [Authorize]
    public virtual async Task UnbindAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for WeChat unbinding.");
        }

        try
        {
            await _rateLimiter.CheckAsync(
                "unbind",
                BuildCurrentUserRateLimitSubject(CurrentUser.Id.Value, null));
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.WeChatUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToWechatError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.Value.ToString());
        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            await RecordAuditAsync(
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.WeChatUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: WechatLoginConsts.UnbindNotAllowedError,
                clientId: null,
                deviceIdHash: null);

            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatUnbindNotAllowed)
                .WithData("error", WechatLoginConsts.UnbindNotAllowedError);
        }

        var binding = await FindEnabledBindingByUserAsync(CurrentUser.Id.Value);
        if (binding == null)
        {
            await RecordAuditAsync(
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.WeChatUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: WechatLoginConsts.BindingNotFoundError,
                clientId: null,
                deviceIdHash: null);
            return;
        }

        binding.Disable();
        await _bindingRepository.UpdateAsync(binding, autoSave: true);

        await RecordAuditAsync(
            userId: CurrentUser.Id,
            userName: CurrentUser.UserName,
            action: MobileAuthAuditLogActions.WeChatUnbind,
            result: MobileAuthAuditLogResults.Success,
            failureReason: null,
            clientId: null,
            deviceIdHash: null);
    }

    [AllowAnonymous]
    public virtual async Task<WechatLoginResult> LoginAsync(WechatLoginInput input)
    {
        WechatIdentity identity;
        try
        {
            await _rateLimiter.CheckAsync(
                "login",
                BuildLoginRateLimitSubject(input));

            identity = await ExchangeIdentityAsync(input.Code, input.Platform);
        }
        catch (BusinessException exception)
        {
            var error = ToWechatError(exception);
            await RecordAuditAsync(
                userId: null,
                userName: null,
                action: MobileAuthAuditLogActions.WeChatLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: error,
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return WechatLoginResult.Failure(error, "WeChat login is unavailable.");
        }

        var binding = await FindBindingByIdentityAsync(identity);
        if (binding == null || !binding.IsEnabled)
        {
            var ticket = await _ticketStore.CreateAsync(identity, GetBindingTicketLifetime());
            await RecordAuditAsync(
                userId: null,
                userName: null,
                action: MobileAuthAuditLogActions.WeChatLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: WechatLoginConsts.BindingRequiredError,
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return WechatLoginResult.Failure(
                WechatLoginConsts.BindingRequiredError,
                "WeChat account is not bound to a PrivateCloudDrive user.",
                ticket);
        }

        var user = await _userManager.FindByIdAsync(binding.UserId.ToString());
        if (user == null || !user.IsActive)
        {
            await RecordAuditAsync(
                userId: binding.UserId,
                userName: null,
                action: MobileAuthAuditLogActions.WeChatLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "invalid_user",
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return WechatLoginResult.Failure("invalid_grant", "The bound user cannot sign in.");
        }

        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            await RecordAuditAsync(
                userId: binding.UserId,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.WeChatLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "user_locked_out",
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return WechatLoginResult.Failure("invalid_grant", "The bound user cannot sign in.");
        }

        binding.UpdateProfile(identity.UnionId, identity.NickName, identity.AvatarUrl);
        binding.MarkLogin(Clock.Now);
        await _bindingRepository.UpdateAsync(binding, autoSave: true);

        await RecordAuditAsync(
            userId: user.Id,
            userName: user.UserName,
            action: MobileAuthAuditLogActions.WeChatLogin,
            result: MobileAuthAuditLogResults.Success,
            failureReason: null,
            clientId: input.ClientId,
            deviceIdHash: input.DeviceIdHash);

        return WechatLoginResult.Success(user.Id, user.UserName);
    }

    private async Task<WechatIdentity> ExchangeIdentityAsync(string code, string? platform)
    {
        if (!_options.IsUsable())
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatDisabled)
                .WithData("error", WechatLoginConsts.DisabledError);
        }

        return await _wechatIdentityService.ExchangeAsync(code, platform);
    }

    private async Task<WechatUserBinding> BindIdentityToUserAsync(WechatIdentity identity, Guid userId)
    {
        var existingBinding = await FindBindingByIdentityAsync(identity);
        if (existingBinding != null)
        {
            if (existingBinding.UserId != userId)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatAlreadyBound)
                    .WithData("error", WechatLoginConsts.AlreadyBoundError);
            }

            existingBinding.UpdateProfile(identity.UnionId, identity.NickName, identity.AvatarUrl);
            existingBinding.Enable();
            await _bindingRepository.UpdateAsync(existingBinding, autoSave: true);

            return existingBinding;
        }

        var binding = new WechatUserBinding(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            userId,
            identity.AppId,
            identity.OpenId,
            identity.UnionId,
            identity.NickName,
            identity.AvatarUrl);

        await _bindingRepository.InsertAsync(binding, autoSave: true);

        return binding;
    }

    private async Task<WechatUserBinding?> FindBindingByIdentityAsync(WechatIdentity identity)
    {
        var queryable = await _bindingRepository.GetQueryableAsync();
        var hasUnionId = !string.IsNullOrWhiteSpace(identity.UnionId);

        return await _asyncExecuter.FirstOrDefaultAsync(
            queryable.Where(binding =>
                binding.TenantId == CurrentTenant.Id &&
                (
                    (binding.AppId == identity.AppId && binding.OpenId == identity.OpenId) ||
                    (hasUnionId && binding.UnionId == identity.UnionId)
                )));
    }

    private async Task<WechatUserBinding?> FindEnabledBindingByUserAsync(Guid userId)
    {
        var queryable = await _bindingRepository.GetQueryableAsync();
        return await _asyncExecuter.FirstOrDefaultAsync(
            queryable
                .Where(binding =>
                    binding.TenantId == CurrentTenant.Id &&
                    binding.UserId == userId &&
                    binding.AppId == _options.AppId &&
                    binding.IsEnabled)
                .OrderByDescending(binding => binding.CreationTime));
    }

    private async Task<IdentityUser?> FindUserByNameOrEmailAsync(string userNameOrEmail)
    {
        var normalized = userNameOrEmail.Trim();
        if (normalized.Contains('@', StringComparison.Ordinal))
        {
            return await _userManager.FindByEmailAsync(normalized);
        }

        return await _userManager.FindByNameAsync(normalized);
    }

    private async Task RecordAuditAsync(
        Guid? userId,
        string? userName,
        string action,
        string result,
        string? failureReason,
        string? clientId,
        string? deviceIdHash)
    {
        var auditLog = new MobileAuthAuditLog(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            userId,
            userName,
            MobileAuthAuditLogProviders.WeChat,
            action,
            result,
            failureReason,
            clientId,
            deviceIdHash,
            userAgent: null);

        await _auditLogRepository.InsertAsync(auditLog, autoSave: true);
    }

    private TimeSpan GetBindingTicketLifetime()
    {
        return TimeSpan.FromMinutes(Math.Max(1, _options.BindingTicketLifetimeMinutes));
    }

    private static string BuildLoginRateLimitSubject(WechatLoginInput input)
    {
        var clientId = NormalizeRateLimitPart(input.ClientId, "anonymous-client");
        var deviceIdHash = NormalizeRateLimitPart(input.DeviceIdHash, "anonymous-device");
        var platform = NormalizeRateLimitPart(input.Platform, "unknown-platform");

        return $"client:{clientId}|device:{deviceIdHash}|platform:{platform}";
    }

    private static string BuildCurrentUserRateLimitSubject(Guid userId, string? deviceIdHash)
    {
        return $"user:{userId:N}|device:{NormalizeRateLimitPart(deviceIdHash, "current-session")}";
    }

    private static string BuildBindExistingRateLimitSubject(string userNameOrEmail)
    {
        return $"account:{NormalizeRateLimitPart(userNameOrEmail, "unknown-account").ToUpperInvariant()}";
    }

    private static string NormalizeRateLimitPart(string? value, string fallback)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string ToWechatError(BusinessException exception)
    {
        if (exception.Data.Contains("error") && exception.Data["error"] is string error)
        {
            return error;
        }

        return exception.Code switch
        {
            PrivateCloudDriveDomainErrorCodes.WeChatDisabled => WechatLoginConsts.DisabledError,
            PrivateCloudDriveDomainErrorCodes.WeChatCodeExchangeFailed => WechatLoginConsts.CodeExchangeFailedError,
            PrivateCloudDriveDomainErrorCodes.WeChatAlreadyBound => WechatLoginConsts.AlreadyBoundError,
            PrivateCloudDriveDomainErrorCodes.WeChatBindingTicketNotFound => WechatLoginConsts.BindingTicketNotFoundError,
            PrivateCloudDriveDomainErrorCodes.WeChatUnbindNotAllowed => WechatLoginConsts.UnbindNotAllowedError,
            PrivateCloudDriveDomainErrorCodes.WeChatRateLimited => WechatLoginConsts.RateLimitedError,
            _ => "wechat_error"
        };
    }

    private static WechatBindingDto ToDto(WechatUserBinding binding)
    {
        return new WechatBindingDto
        {
            Id = binding.Id,
            TenantId = binding.TenantId,
            UserId = binding.UserId,
            AppId = binding.AppId,
            NickName = binding.NickName,
            AvatarUrl = binding.AvatarUrl,
            IsEnabled = binding.IsEnabled,
            LastLoginTime = binding.LastLoginTime,
            CreationTime = binding.CreationTime
        };
    }
}
