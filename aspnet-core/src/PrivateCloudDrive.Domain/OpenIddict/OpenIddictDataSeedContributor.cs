using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using PrivateCloudDrive.MobileAuth;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace PrivateCloudDrive.OpenIddict;

/* Creates initial data that is needed to property run the application
 * and make client-to-server communication possible.
 */
/// <summary>
/// 表示OpenIddictDataSeedContributor组件，封装对应业务场景的状态或行为。
/// </summary>
public class OpenIddictDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationRepository _openIddictApplicationRepository;
    private readonly IAbpApplicationManager _applicationManager;
    private readonly IOpenIddictScopeRepository _openIddictScopeRepository;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IStringLocalizer<OpenIddictResponse> L;

    /// <summary>
    /// 初始化 <see cref="OpenIddictDataSeedContributor"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public OpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeRepository openIddictScopeRepository,
        IOpenIddictScopeManager scopeManager,
        IPermissionDataSeeder permissionDataSeeder,
        IStringLocalizer<OpenIddictResponse> l )
    {
        _configuration = configuration;
        _openIddictApplicationRepository = openIddictApplicationRepository;
        _applicationManager = applicationManager;
        _openIddictScopeRepository = openIddictScopeRepository;
        _scopeManager = scopeManager;
        _permissionDataSeeder = permissionDataSeeder;
        L = l;
    }

    /// <summary>
    /// 初始化种子数据，确保运行或测试环境具备必要的基础配置。
    /// </summary>
    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateScopesAsync();
        await CreateApplicationsAsync();
    }

    private async Task CreateScopesAsync()
    {
        if (await _openIddictScopeRepository.FindByNameAsync("PrivateCloudDrive") == null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor {
                Name = "PrivateCloudDrive", DisplayName = "PrivateCloudDrive API", Resources = { "PrivateCloudDrive" }
            });
        }
    }

    private async Task CreateApplicationsAsync()
    {
        var commonScopes = new List<string> {
            OpenIddictConstants.Permissions.Scopes.Address,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            "PrivateCloudDrive"
        };

        var configurationSection = _configuration.GetSection("OpenIddict:Applications");






        // Swagger Client
        var swaggerClientId = configurationSection["PrivateCloudDrive_Swagger:ClientId"];
        if (!swaggerClientId.IsNullOrWhiteSpace())
        {
            var swaggerRootUrl = configurationSection["PrivateCloudDrive_Swagger:RootUrl"]?.TrimEnd('/');

            await CreateApplicationAsync(
                name: swaggerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: null,
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode, },
                scopes: commonScopes,
                redirectUri: $"{swaggerRootUrl}/swagger/oauth2-redirect.html",
                clientUri: swaggerRootUrl
            );
        }

        var appConfigurationSection = configurationSection.GetSection("PrivateCloudDrive_App");
        var appClientId = appConfigurationSection["ClientId"];
        if (!appClientId.IsNullOrWhiteSpace())
        {
            var appRedirectUris = GetConfiguredUris(appConfigurationSection, "RedirectUri", "RedirectUris");
            var appPostLogoutRedirectUris = GetConfiguredUris(
                appConfigurationSection,
                "PostLogoutRedirectUri",
                "PostLogoutRedirectUris");
            var appScopes = commonScopes.Concat(new[] { "openid", "offline_access" }).ToList();

            await CreateApplicationAsync(
                name: appClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "PrivateCloudDrive MAUI App",
                secret: null,
                grantTypes: new List<string>
                {
                    OpenIddictConstants.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.GrantTypes.Password,
                    OpenIddictConstants.GrantTypes.RefreshToken,
                    WechatLoginConsts.GrantType,
                    ExternalLoginConsts.GrantType
                },
                scopes: appScopes,
                redirectUris: appRedirectUris,
                postLogoutRedirectUris: appPostLogoutRedirectUris
            );
        }
    }

    private async Task CreateApplicationAsync(
        [NotNull] string name,
        [NotNull] string type,
        [NotNull] string consentType,
        string displayName,
        string? secret,
        List<string> grantTypes,
        List<string> scopes,
        string? clientUri = null,
        string? redirectUri = null,
        string? postLogoutRedirectUri = null,
        List<string>? redirectUris = null,
        List<string>? postLogoutRedirectUris = null,
        List<string>? permissions = null)
    {
        if (!string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Public,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["NoClientSecretCanBeSetForPublicApplications"]);
        }

        if (string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Confidential,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["TheClientSecretIsRequiredForConfidentialApplications"]);
        }

        var client = await _openIddictApplicationRepository.FindByClientIdAsync(name);

        var application = new AbpApplicationDescriptor {
            ClientId = name,
            ClientType = type,
            ClientSecret = secret,
            ConsentType = consentType,
            DisplayName = displayName,
            ClientUri = clientUri,
        };

        Check.NotNullOrEmpty(grantTypes, nameof(grantTypes));
        Check.NotNullOrEmpty(scopes, nameof(scopes));

        if (new[] { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.Implicit }.All(
                grantTypes.Contains))
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

            if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
            }
        }

        if (!redirectUri.IsNullOrWhiteSpace() ||
            !postLogoutRedirectUri.IsNullOrWhiteSpace() ||
            redirectUris?.Count > 0 ||
            postLogoutRedirectUris?.Count > 0)
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        }

        var buildInGrantTypes = new[] {
            OpenIddictConstants.GrantTypes.Implicit, OpenIddictConstants.GrantTypes.Password,
            OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials,
            OpenIddictConstants.GrantTypes.DeviceCode, OpenIddictConstants.GrantTypes.RefreshToken
        };

        foreach (var grantType in grantTypes)
        {
            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.ClientCredentials ||
                grantType == OpenIddictConstants.GrantTypes.Password ||
                grantType == OpenIddictConstants.GrantTypes.RefreshToken ||
                grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            }

            if (grantType == OpenIddictConstants.GrantTypes.ClientCredentials)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Password)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }

            if (grantType == OpenIddictConstants.GrantTypes.RefreshToken)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }

            if (grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.DeviceCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.DeviceAuthorization);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);
                if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }

            if (!buildInGrantTypes.Contains(grantType))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.GrantType + grantType);
            }
        }

        var buildInScopes = new[] {
            OpenIddictConstants.Permissions.Scopes.Address, OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone, OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles
        };

        foreach (var scope in scopes)
        {
            if (buildInScopes.Contains(scope))
            {
                application.Permissions.Add(scope);
            }
            else
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }
        }

        var configuredRedirectUris = GetDistinctUris(redirectUri, redirectUris);
        foreach (var configuredRedirectUri in configuredRedirectUris)
        {
            if (!Uri.TryCreate(configuredRedirectUri, UriKind.Absolute, out var uri) ||
                !uri.IsWellFormedOriginalString())
            {
                throw new BusinessException(L["InvalidRedirectUri", configuredRedirectUri]);
            }

            if (application.RedirectUris.All(x => x != uri))
            {
                application.RedirectUris.Add(uri);
            }
        }

        var configuredPostLogoutRedirectUris = GetDistinctUris(postLogoutRedirectUri, postLogoutRedirectUris);
        foreach (var configuredPostLogoutRedirectUri in configuredPostLogoutRedirectUris)
        {
            if (!Uri.TryCreate(configuredPostLogoutRedirectUri, UriKind.Absolute, out var uri) ||
                !uri.IsWellFormedOriginalString())
            {
                throw new BusinessException(L["InvalidPostLogoutRedirectUri", configuredPostLogoutRedirectUri]);
            }

            if (application.PostLogoutRedirectUris.All(x => x != uri))
            {
                application.PostLogoutRedirectUris.Add(uri);
            }
        }

        if (permissions != null)
        {
            await _permissionDataSeeder.SeedAsync(
                ClientPermissionValueProvider.ProviderName,
                name,
                permissions,
                null
            );
        }

        if (client == null)
        {
            await _applicationManager.CreateAsync(application);
            return;
        }

        var shouldUpdateClient = false;

        if (!HasSameRedirectUris(client, application) ||
            !HasSamePostLogoutRedirectUris(client, application))
        {
            client.RedirectUris = SerializeValues(application.RedirectUris.Select(q => q.ToString()));
            client.PostLogoutRedirectUris = SerializeValues(application.PostLogoutRedirectUris.Select(q => q.ToString()));
            shouldUpdateClient = true;
        }

        if (!HasSamePermissions(client, application))
        {
            client.Permissions = SerializeValues(application.Permissions.Select(q => q.ToString()));
            shouldUpdateClient = true;
        }

        if (shouldUpdateClient)
        {
            await _openIddictApplicationRepository.UpdateAsync(client, autoSave: true);
        }
    }

    private bool HasSameRedirectUris(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.RedirectUris == SerializeValues(application.RedirectUris.Select(q => q.ToString()));
    }

    private bool HasSamePostLogoutRedirectUris(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.PostLogoutRedirectUris == SerializeValues(application.PostLogoutRedirectUris.Select(q => q.ToString()));
    }

    private bool HasSamePermissions(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.Permissions == SerializeValues(application.Permissions.Select(q => q.ToString()));
    }

    private static string SerializeValues(IEnumerable<string> values)
    {
        return JsonSerializer.Serialize(values.Select(value => value.TrimEnd('/')));
    }

    private static List<string> GetConfiguredUris(
        IConfigurationSection configurationSection,
        string singleUriKey,
        string pluralUrisKey)
    {
        var uris = new List<string>();
        var singleUri = configurationSection[singleUriKey];
        if (!singleUri.IsNullOrWhiteSpace())
        {
            uris.Add(singleUri!);
        }

        uris.AddRange(
            configurationSection
                .GetSection(pluralUrisKey)
                .GetChildren()
                .Select(child => child.Value)
                .Where(uri => !uri.IsNullOrWhiteSpace())
                .Select(uri => uri!));

        return uris
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetDistinctUris(string? uri, List<string>? uris)
    {
        var values = new List<string>();
        if (!uri.IsNullOrWhiteSpace())
        {
            values.Add(uri!);
        }

        if (uris != null)
        {
            values.AddRange(uris.Where(value => !value.IsNullOrWhiteSpace()));
        }

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
