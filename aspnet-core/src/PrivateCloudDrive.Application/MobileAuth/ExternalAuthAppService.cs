using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
using Volo.Abp.PermissionManagement;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 通用第三方登录应用服务，负责 Provider 配置下发、账号绑定、解绑、自动建号和审计记录。
/// 安全边界：不返回 Provider Secret，不持久化 Provider Token，不把授权码或密码写入审计日志。
/// </summary>
[ExposeServices(
    typeof(IExternalAuthAppService),
    typeof(IExternalLoginService),
    typeof(ExternalAuthAppService))]
public class ExternalAuthAppService :
    PrivateCloudDriveAppService,
    IExternalAuthAppService,
    IExternalLoginService
{
    private const string ExternalUserEmailDomain = "external.privateclouddrive.local";
    private const int AutoUserNameMaxLength = 64;
    private const int AutoUserNameBaseMaxLength = 40;
    private static readonly string[] ExternalUserDefaultPermissions =
    [
        PrivateCloudDrivePermissions.FileCenter.Default,
        PrivateCloudDrivePermissions.FileCenter.View,
        PrivateCloudDrivePermissions.FileCenter.Upload,
        PrivateCloudDrivePermissions.FileCenter.Download,
        PrivateCloudDrivePermissions.FileCenter.Delete,
        PrivateCloudDrivePermissions.FileCenter.Manage
    ];

    private readonly ExternalLoginOptions _options;
    private readonly IExternalIdentityService _externalIdentityService;
    private readonly IExternalBindingTicketStore _ticketStore;
    private readonly IExternalAuthRateLimiter _rateLimiter;
    private readonly IRepository<ExternalUserBinding, Guid> _bindingRepository;
    private readonly IRepository<MobileAuthAuditLog, Guid> _auditLogRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="ExternalAuthAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public ExternalAuthAppService(
        IOptions<ExternalLoginOptions> options,
        IExternalIdentityService externalIdentityService,
        IExternalBindingTicketStore ticketStore,
        IExternalAuthRateLimiter rateLimiter,
        IRepository<ExternalUserBinding, Guid> bindingRepository,
        IRepository<MobileAuthAuditLog, Guid> auditLogRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _options = options.Value;
        _externalIdentityService = externalIdentityService;
        _ticketStore = ticketStore;
        _rateLimiter = rateLimiter;
        _bindingRepository = bindingRepository;
        _auditLogRepository = auditLogRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 返回客户端可见的第三方登录开关和授权端点配置。
    /// </summary>
    [AllowAnonymous]
    public virtual Task<ExternalLoginSettingsDto> GetSettingsAsync()
    {
        return Task.FromResult(new ExternalLoginSettingsDto
        {
            Providers =
            [
                ToProviderSettings(
                    ExternalLoginConsts.GoogleProviderName,
                    "Google",
                    _options.Google,
                    requireClientSecret: false),
                ToProviderSettings(
                    ExternalLoginConsts.GitHubProviderName,
                    "GitHub",
                    _options.GitHub,
                    requireClientSecret: true)
            ]
        });
    }

    /// <summary>
    /// 查询当前用户启用中的第三方账号绑定，用于设置页展示。
    /// </summary>
    [Authorize]
    public virtual async Task<IReadOnlyList<ExternalBindingDto>> GetBindingsAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            return [];
        }

        var queryable = await _bindingRepository.GetQueryableAsync();
        var bindings = await _asyncExecuter.ToListAsync(
            queryable
                .Where(binding =>
                    binding.TenantId == CurrentTenant.Id &&
                    binding.UserId == CurrentUser.Id.Value &&
                    binding.IsEnabled)
                .OrderBy(binding => binding.Provider)
                .ThenByDescending(binding => binding.CreationTime));

        return bindings.Select(ToDto).ToList();
    }

    /// <summary>
    /// 已登录用户主动绑定第三方账号。
    /// </summary>
    [Authorize]
    public virtual async Task<ExternalBindingDto> BindCurrentAsync(BindCurrentExternalLoginInput input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for external account binding.");
        }

        var provider = NormalizeProviderOrThrow(input.Provider);

        try
        {
            await _rateLimiter.CheckAsync(
                "bind-current",
                BuildCurrentUserRateLimitSubject(provider, CurrentUser.Id.Value, input.DeviceIdHash));

            var identity = await ExchangeIdentityAsync(provider, input.Code, input.RedirectUri, input.CodeVerifier);
            var binding = await BindIdentityToUserAsync(identity, CurrentUser.Id.Value);

            await RecordAuditAsync(
                provider,
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Success,
                failureReason: null,
                clientId: null,
                deviceIdHash: input.DeviceIdHash);

            return ToDto(binding);
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                provider,
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToExternalError(exception),
                clientId: null,
                deviceIdHash: input.DeviceIdHash);
            throw;
        }
    }

    /// <summary>
    /// 使用短期绑定票据和账号密码，把首次第三方登录身份绑定到已有用户。
    /// </summary>
    [AllowAnonymous]
    public virtual async Task<ExternalBindingDto> BindExistingAsync(BindExistingExternalLoginInput input)
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
                provider: null,
                userId: null,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToExternalError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }

        var identity = await _ticketStore.GetAsync(input.BindingTicket);
        if (identity == null)
        {
            await RecordAuditAsync(
                provider: null,
                userId: null,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ExternalLoginConsts.BindingTicketNotFoundError,
                clientId: null,
                deviceIdHash: null);

            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginBindingTicketNotFound)
                .WithData("error", ExternalLoginConsts.BindingTicketNotFoundError);
        }

        var user = await FindUserByNameOrEmailAsync(input.UserNameOrEmail);
        if (user == null || !user.IsActive)
        {
            await RecordAuditAsync(
                identity.Provider,
                userId: user?.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "invalid_user_credentials",
                clientId: null,
                deviceIdHash: null);

            throw new AbpAuthorizationException("Invalid username or password.");
        }

        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            await RecordAuditAsync(
                identity.Provider,
                userId: user.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.ExternalBind,
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
                identity.Provider,
                userId: user.Id,
                userName: input.UserNameOrEmail,
                action: MobileAuthAuditLogActions.ExternalBind,
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
                identity.Provider,
                userId: user.Id,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Success,
                failureReason: null,
                clientId: null,
                deviceIdHash: null);

            return ToDto(binding);
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                identity.Provider,
                userId: user.Id,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToExternalError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }
    }

    /// <summary>
    /// 软解绑当前用户的指定第三方账号。
    /// 若当前用户没有密码登录能力，则禁止解绑，避免账号失去所有登录方式。
    /// </summary>
    [Authorize]
    public virtual async Task UnbindAsync(string provider)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for external account unbinding.");
        }

        var normalizedProvider = NormalizeProviderOrThrow(provider);

        try
        {
            await _rateLimiter.CheckAsync(
                "unbind",
                BuildCurrentUserRateLimitSubject(normalizedProvider, CurrentUser.Id.Value, null));
        }
        catch (BusinessException exception)
        {
            await RecordAuditAsync(
                normalizedProvider,
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.ExternalUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ToExternalError(exception),
                clientId: null,
                deviceIdHash: null);
            throw;
        }

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.Value.ToString());
        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            await RecordAuditAsync(
                normalizedProvider,
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.ExternalUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ExternalLoginConsts.UnbindNotAllowedError,
                clientId: null,
                deviceIdHash: null);

            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginUnbindNotAllowed)
                .WithData("error", ExternalLoginConsts.UnbindNotAllowedError);
        }

        var binding = await FindEnabledBindingByUserAsync(CurrentUser.Id.Value, normalizedProvider);
        if (binding == null)
        {
            await RecordAuditAsync(
                normalizedProvider,
                userId: CurrentUser.Id,
                userName: CurrentUser.UserName,
                action: MobileAuthAuditLogActions.ExternalUnbind,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: ExternalLoginConsts.BindingNotFoundError,
                clientId: null,
                deviceIdHash: null);
            return;
        }

        binding.Disable();
        await _bindingRepository.UpdateAsync(binding, autoSave: true);

        await RecordAuditAsync(
            normalizedProvider,
            userId: CurrentUser.Id,
            userName: CurrentUser.UserName,
            action: MobileAuthAuditLogActions.ExternalUnbind,
            result: MobileAuthAuditLogResults.Success,
            failureReason: null,
            clientId: null,
            deviceIdHash: null);
    }

    /// <summary>
    /// OpenIddict 扩展 grant 的核心登录流程：校验限流、换取 Provider 身份、查找/创建绑定并返回签发令牌所需用户。
    /// </summary>
    [AllowAnonymous]
    public virtual async Task<ExternalLoginResult> LoginAsync(ExternalLoginInput input)
    {
        var provider = ExternalLoginConsts.NormalizeProvider(input.Provider);
        if (provider == null)
        {
            return ExternalLoginResult.Failure(
                ExternalLoginConsts.ProviderUnsupportedError,
                "External login provider is unsupported.");
        }

        ExternalIdentity identity;
        try
        {
            await _rateLimiter.CheckAsync(
                "login",
                BuildLoginRateLimitSubject(input, provider));

            identity = await ExchangeIdentityAsync(provider, input.Code, input.RedirectUri, input.CodeVerifier);
        }
        catch (BusinessException exception)
        {
            var error = ToExternalError(exception);
            var errorDescription = ToExternalErrorDescription(exception);
            await RecordAuditAsync(
                provider,
                userId: null,
                userName: null,
                action: MobileAuthAuditLogActions.ExternalLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: NormalizeFailureReason($"{error}: {errorDescription}"),
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return ExternalLoginResult.Failure(error, errorDescription);
        }

        var binding = await FindBindingByIdentityAsync(identity);
        if (binding == null)
        {
            var autoUser = await CreateExternalUserAsync(identity);
            binding = await BindIdentityToUserAsync(identity, autoUser.Id);

            await RecordAuditAsync(
                provider,
                userId: autoUser.Id,
                userName: autoUser.UserName,
                action: MobileAuthAuditLogActions.ExternalBind,
                result: MobileAuthAuditLogResults.Success,
                failureReason: "auto_provisioned",
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);
        }
        else if (!binding.IsEnabled)
        {
            binding.Enable();
        }

        var user = await _userManager.FindByIdAsync(binding.UserId.ToString());
        if (user == null || !user.IsActive)
        {
            await RecordAuditAsync(
                provider,
                userId: binding.UserId,
                userName: null,
                action: MobileAuthAuditLogActions.ExternalLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "invalid_user",
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return ExternalLoginResult.Failure("invalid_grant", "The bound user cannot sign in.");
        }

        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            await RecordAuditAsync(
                provider,
                userId: binding.UserId,
                userName: user.UserName,
                action: MobileAuthAuditLogActions.ExternalLogin,
                result: MobileAuthAuditLogResults.Failed,
                failureReason: "user_locked_out",
                clientId: input.ClientId,
                deviceIdHash: input.DeviceIdHash);

            return ExternalLoginResult.Failure("invalid_grant", "The bound user cannot sign in.");
        }

        await EnsureExternalUserDefaultPermissionsAsync(user.Id);

        binding.UpdateProfile(identity.Email, identity.DisplayName, identity.AvatarUrl);
        binding.MarkLogin(Clock.Now);
        await _bindingRepository.UpdateAsync(binding, autoSave: true);

        await RecordAuditAsync(
            provider,
            userId: user.Id,
            userName: user.UserName,
            action: MobileAuthAuditLogActions.ExternalLogin,
            result: MobileAuthAuditLogResults.Success,
            failureReason: null,
            clientId: input.ClientId,
            deviceIdHash: input.DeviceIdHash);

        return ExternalLoginResult.Success(user.Id, user.UserName);
    }

    private async Task<ExternalIdentity> ExchangeIdentityAsync(
        string provider,
        string code,
        string redirectUri,
        string? codeVerifier)
    {
        var providerOptions = _options.GetProvider(provider);
        var requireClientSecret = provider == ExternalLoginConsts.GitHubProviderName;
        if (providerOptions == null || !providerOptions.IsUsable(requireClientSecret))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginDisabled)
                .WithData("error", ExternalLoginConsts.DisabledError);
        }

        return await _externalIdentityService.ExchangeAsync(provider, code, redirectUri, codeVerifier);
    }

    /// <summary>
    /// 第三方自动建号用户默认具备 MVP 文件功能权限；重复设置是幂等的，用于修复早期已创建但未授权的账号。
    /// </summary>
    private async Task EnsureExternalUserDefaultPermissionsAsync(Guid userId)
    {
        foreach (var permission in ExternalUserDefaultPermissions)
        {
            await _permissionManager.SetForUserAsync(userId, permission, true);
        }
    }

    /// <summary>
    /// 为首次第三方登录自动创建本地用户。
    /// 当 Provider 邮箱不可用或已占用时，生成系统内部邮箱，避免误绑定到已有账号。
    /// </summary>
    private async Task<IdentityUser> CreateExternalUserAsync(ExternalIdentity identity)
    {
        var userName = await CreateUniqueExternalUserNameAsync(identity);
        var (email, isProviderEmail) = await CreateExternalEmailAsync(identity, userName);
        var user = new IdentityUser(_guidGenerator.Create(), userName, email, CurrentTenant.Id);
        user.SetEmailConfirmed(isProviderEmail);

        var result = await _userManager.CreateAsync(user, GenerateExternalPassword());
        if (result.Succeeded)
        {
            return user;
        }

        throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginAutoProvisionFailed)
            .WithData("error", ExternalLoginConsts.AutoProvisionFailedError)
            .WithData("provider", identity.Provider)
            .WithData(
                "provider_error_description",
                string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private async Task<string> CreateUniqueExternalUserNameAsync(ExternalIdentity identity)
    {
        var provider = identity.Provider.ToLowerInvariant();
        var readablePart = SanitizeUserNamePart(
            FirstNonEmpty(
                GetEmailLocalPart(identity.Email),
                identity.DisplayName,
                identity.ProviderUserId,
                "user"));
        var hash = CreateStableHash($"{identity.Provider}:{identity.ProviderUserId}", length: 10);
        var baseName = TrimUserNamePart($"{provider}-{readablePart}", AutoUserNameBaseMaxLength);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = attempt == 0 ? hash : $"{hash}{attempt}";
            var candidate = TrimUserNamePart($"{baseName}-{suffix}", AutoUserNameMaxLength);
            if (await _userManager.FindByNameAsync(candidate) == null)
            {
                return candidate;
            }
        }

        return TrimUserNamePart(
            $"{provider}-user-{CreateStableHash(Guid.NewGuid().ToString("N"), length: 16)}",
            AutoUserNameMaxLength);
    }

    private async Task<(string Email, bool IsProviderEmail)> CreateExternalEmailAsync(
        ExternalIdentity identity,
        string userName)
    {
        var providerEmail = NormalizeEmail(identity.Email);
        if (providerEmail != null && await _userManager.FindByEmailAsync(providerEmail) == null)
        {
            return (providerEmail, true);
        }

        var syntheticEmail = $"{userName}@{ExternalUserEmailDomain}";
        if (await _userManager.FindByEmailAsync(syntheticEmail) == null)
        {
            return (syntheticEmail, false);
        }

        return ($"{userName}-{CreateStableHash(Guid.NewGuid().ToString("N"), length: 8)}@{ExternalUserEmailDomain}", false);
    }

    /// <summary>
    /// 建立或恢复第三方身份与本地用户的绑定；同一 ProviderUserId 不允许绑定到多个用户。
    /// </summary>
    private async Task<ExternalUserBinding> BindIdentityToUserAsync(ExternalIdentity identity, Guid userId)
    {
        var existingBinding = await FindBindingByIdentityAsync(identity);
        if (existingBinding != null)
        {
            if (existingBinding.UserId != userId)
            {
                throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginAlreadyBound)
                    .WithData("error", ExternalLoginConsts.AlreadyBoundError);
            }

            existingBinding.UpdateProfile(identity.Email, identity.DisplayName, identity.AvatarUrl);
            existingBinding.Enable();
            await _bindingRepository.UpdateAsync(existingBinding, autoSave: true);

            return existingBinding;
        }

        var binding = new ExternalUserBinding(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            userId,
            identity.Provider,
            identity.ProviderUserId,
            identity.Email,
            identity.DisplayName,
            identity.AvatarUrl);

        await _bindingRepository.InsertAsync(binding, autoSave: true);

        return binding;
    }

    private async Task<ExternalUserBinding?> FindBindingByIdentityAsync(ExternalIdentity identity)
    {
        var queryable = await _bindingRepository.GetQueryableAsync();
        return await _asyncExecuter.FirstOrDefaultAsync(
            queryable.Where(binding =>
                binding.TenantId == CurrentTenant.Id &&
                binding.Provider == identity.Provider &&
                binding.ProviderUserId == identity.ProviderUserId));
    }

    private async Task<ExternalUserBinding?> FindEnabledBindingByUserAsync(Guid userId, string provider)
    {
        var queryable = await _bindingRepository.GetQueryableAsync();
        return await _asyncExecuter.FirstOrDefaultAsync(
            queryable
                .Where(binding =>
                    binding.TenantId == CurrentTenant.Id &&
                    binding.UserId == userId &&
                    binding.Provider == provider &&
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
        string? provider,
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
            NormalizeProviderForAudit(provider),
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

    private static ExternalLoginProviderSettingsDto ToProviderSettings(
        string provider,
        string displayName,
        ExternalLoginProviderOptions options,
        bool requireClientSecret)
    {
        var isEnabled = options.IsUsable(requireClientSecret);
        return new ExternalLoginProviderSettingsDto
        {
            Provider = provider,
            DisplayName = displayName,
            IsEnabled = isEnabled,
            ClientId = isEnabled ? options.ClientId : null,
            AuthorizationEndpoint = options.AuthorizationEndpoint,
            Scope = options.Scope,
            RedirectUri = options.RedirectUri,
            UsePkce = options.UsePkce
        };
    }

    private static string NormalizeProviderOrThrow(string provider)
    {
        return ExternalLoginConsts.NormalizeProvider(provider) ??
               throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginProviderUnsupported)
                   .WithData("error", ExternalLoginConsts.ProviderUnsupportedError);
    }

    private static string NormalizeProviderForAudit(string? provider)
    {
        return ExternalLoginConsts.NormalizeProvider(provider) ?? "External";
    }

    private static string BuildLoginRateLimitSubject(ExternalLoginInput input, string provider)
    {
        var clientId = NormalizeRateLimitPart(input.ClientId, "anonymous-client");
        var deviceIdHash = NormalizeRateLimitPart(input.DeviceIdHash, "anonymous-device");

        return $"provider:{provider}|client:{clientId}|device:{deviceIdHash}";
    }

    private static string BuildCurrentUserRateLimitSubject(string provider, Guid userId, string? deviceIdHash)
    {
        return $"provider:{provider}|user:{userId:N}|device:{NormalizeRateLimitPart(deviceIdHash, "current-session")}";
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

    private static string ToExternalError(BusinessException exception)
    {
        if (exception.Data.Contains("error") && exception.Data["error"] is string error)
        {
            return error;
        }

        return exception.Code switch
        {
            PrivateCloudDriveDomainErrorCodes.ExternalLoginDisabled => ExternalLoginConsts.DisabledError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginCodeExchangeFailed => ExternalLoginConsts.CodeExchangeFailedError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginAlreadyBound => ExternalLoginConsts.AlreadyBoundError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginBindingTicketNotFound => ExternalLoginConsts.BindingTicketNotFoundError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginUnbindNotAllowed => ExternalLoginConsts.UnbindNotAllowedError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginRateLimited => ExternalLoginConsts.RateLimitedError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginProviderUnsupported => ExternalLoginConsts.ProviderUnsupportedError,
            PrivateCloudDriveDomainErrorCodes.ExternalLoginAutoProvisionFailed => ExternalLoginConsts.AutoProvisionFailedError,
            _ => "external_login_error"
        };
    }

    private static string ToExternalErrorDescription(BusinessException exception)
    {
        var details = new List<string>();
        AddExternalErrorDetail(details, exception, "provider", "provider");
        AddExternalErrorDetail(details, exception, "provider_status", "status");
        AddExternalErrorDetail(details, exception, "provider_error", "error");
        AddExternalErrorDetail(details, exception, "provider_error_description", "description");

        if (details.Count == 0)
        {
            return "External login is unavailable.";
        }

        return $"External login is unavailable. {string.Join("; ", details)}";
    }

    private static void AddExternalErrorDetail(
        ICollection<string> details,
        BusinessException exception,
        string dataKey,
        string displayKey)
    {
        if (exception.Data.Contains(dataKey) &&
            exception.Data[dataKey] is string value &&
            !string.IsNullOrWhiteSpace(value))
        {
            details.Add($"{displayKey}={value.Trim()}");
        }
    }

    private static string NormalizeFailureReason(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= 512 ? normalized : normalized[..512];
    }

    private static string? NormalizeEmail(string? email)
    {
        var normalized = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Any(char.IsWhiteSpace) ||
            !normalized.Contains('@', StringComparison.Ordinal))
        {
            return null;
        }

        return normalized;
    }

    private static string? GetEmailLocalPart(string? email)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
        {
            return null;
        }

        var atIndex = normalized.IndexOf('@', StringComparison.Ordinal);
        return atIndex <= 0 ? null : normalized[..atIndex];
    }

    private static string SanitizeUserNamePart(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousDash = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
                continue;
            }

            if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "user" : normalized;
    }

    private static string TrimUserNamePart(string value, int maxLength)
    {
        var trimmed = value.Trim('-');
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength].Trim('-');
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "user";
    }

    private static string CreateStableHash(string value, int length)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant()[..length];
    }

    private static string GenerateExternalPassword()
    {
        return $"Aa1!{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";
    }

    private static ExternalBindingDto ToDto(ExternalUserBinding binding)
    {
        return new ExternalBindingDto
        {
            Id = binding.Id,
            TenantId = binding.TenantId,
            UserId = binding.UserId,
            Provider = binding.Provider,
            Email = binding.Email,
            DisplayName = binding.DisplayName,
            AvatarUrl = binding.AvatarUrl,
            IsEnabled = binding.IsEnabled,
            LastLoginTime = binding.LastLoginTime,
            CreationTime = binding.CreationTime
        };
    }
}
