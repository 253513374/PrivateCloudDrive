using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrivateCloudDrive.EntityFrameworkCore;
using PrivateCloudDrive.Menus;
using PrivateCloudDrive.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi;
using OpenIddict.Server;
using PrivateCloudDrive.MobileAuth;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace PrivateCloudDrive;

/// <summary>
/// 配置PrivateCloudDriveHttpApiHostModule模块依赖、服务注册和框架集成行为。
/// </summary>
[DependsOn(
    typeof(PrivateCloudDriveHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(PrivateCloudDriveApplicationModule),
    typeof(PrivateCloudDriveEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpSwashbuckleModule)
)]
public class PrivateCloudDriveHttpApiHostModule : AbpModule
{
    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var requireHttpsMetadata = configuration.GetValue("AuthServer:RequireHttpsMetadata", true);
        var authority = configuration["AuthServer:Authority"];

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("PrivateCloudDrive");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            if (!string.IsNullOrWhiteSpace(authority))
            {
                builder.SetIssuer(new Uri(authority.TrimEnd('/') + "/"));
            }

            builder.AllowPasswordFlow();
            builder.AllowRefreshTokenFlow();
            builder.AllowCustomFlow(WechatLoginConsts.GrantType);
            builder.AllowCustomFlow(ExternalLoginConsts.GrantType);
            builder.SetRevocationEndpointUris("/connect/revocation");
            builder.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(options =>
            {
                options.UseScopedHandler<PasswordLoginRateLimitValidationHandler>();
            });
            builder.AddEventHandler<OpenIddictServerEvents.ApplyTokenResponseContext>(options =>
            {
                options.UseScopedHandler<PasswordLoginRateLimitResponseHandler>();
            });

            if (!requireHttpsMetadata)
            {
                builder.UseAspNetCore().DisableTransportSecurityRequirement();
            }
        });
    }

    /// <summary>
    /// 配置模块服务、选项或框架扩展点，确保运行时行为符合项目约定。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context);
        ConfigureApiAuthenticationResponses(context);
        ValidateProductionSecuritySettings(configuration, hostingEnvironment);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureNavigation();
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigurePublicShareRateLimiting(context, configuration);
        ConfigureBackgroundJobs(configuration);
        ConfigureTokenExtensionGrants();
        if (IsSwaggerEnabled(configuration, hostingEnvironment))
        {
            ConfigureSwaggerServices(context, configuration);
        }
    }

    private void ConfigureTokenExtensionGrants()
    {
        Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
        {
            options.Grants.Remove(WechatLoginConsts.GrantType);
            options.Grants.Add(WechatLoginConsts.GrantType, new WechatTokenExtensionGrant());

            options.Grants.Remove(ExternalLoginConsts.GrantType);
            options.Grants.Add(ExternalLoginConsts.GrantType, new ExternalTokenExtensionGrant());
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private static void ConfigureApiAuthenticationResponses(ServiceConfigurationContext context)
    {
        context.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = redirectContext =>
            {
                if (IsApiRequest(redirectContext.Request.Path))
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                redirectContext.Response.Redirect(redirectContext.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = redirectContext =>
            {
                if (IsApiRequest(redirectContext.Request.Path))
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                redirectContext.Response.Redirect(redirectContext.RedirectUri);
                return Task.CompletedTask;
            };
        });
    }

    private static bool IsApiRequest(PathString path)
    {
        return path.StartsWithSegments("/api") ||
               path.StartsWithSegments("/connect");
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureNavigation()
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new PrivateCloudDriveMenuContributor());
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<PrivateCloudDriveDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}PrivateCloudDrive.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<PrivateCloudDriveDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}PrivateCloudDrive.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<PrivateCloudDriveApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}PrivateCloudDrive.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<PrivateCloudDriveApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}PrivateCloudDrive.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(PrivateCloudDriveApplicationModule).Assembly);
        });
    }

    private void ConfigureBackgroundJobs(IConfiguration configuration)
    {
        Configure<AbpBackgroundJobOptions>(options =>
        {
            var configuredValue = configuration.GetValue<bool?>("BackgroundJobs:IsJobExecutionEnabled");
            if (configuredValue.HasValue)
            {
                options.IsJobExecutionEnabled = configuredValue.Value;
            }
        });
    }

    private static void ConfigurePublicShareRateLimiting(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("Security:PublicSharePasswordRateLimit:PermitLimit", 10);
        var windowMinutes = configuration.GetValue("Security:PublicSharePasswordRateLimit:WindowMinutes", 10);
        var queueLimit = configuration.GetValue("Security:PublicSharePasswordRateLimit:QueueLimit", 0);

        context.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("PublicSharePassword", httpContext =>
            {
                var token = httpContext.Request.RouteValues.TryGetValue("token", out var routeToken)
                    ? Convert.ToString(routeToken, CultureInfo.InvariantCulture)
                    : "unknown-token";
                var remoteAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
                var partitionKey = $"{remoteAddress}:{token}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(windowMinutes),
                        QueueLimit = queueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });
    }

    private static bool IsSwaggerEnabled(IConfiguration configuration, IHostEnvironment hostingEnvironment)
    {
        return configuration.GetValue<bool?>("Swagger:Enabled") ?? hostingEnvironment.IsDevelopment();
    }

    private static bool AllowsInsecureLocalValidation(IConfiguration configuration)
    {
        return configuration.GetValue("Security:AllowInsecureTransportForLocalValidation", false);
    }

    private static void ValidateProductionSecuritySettings(IConfiguration configuration, IHostEnvironment hostingEnvironment)
    {
        if (!hostingEnvironment.IsProduction() || AllowsInsecureLocalValidation(configuration))
        {
            return;
        }

        var failures = new List<string>();
        if (!configuration.GetValue("AuthServer:RequireHttpsMetadata", true))
        {
            failures.Add("AuthServer:RequireHttpsMetadata=false is forbidden in Production.");
        }

        foreach (var urlKey in new[] { "App:SelfUrl", "AuthServer:Authority" })
        {
            var configuredUrl = configuration[urlKey];
            if (!string.IsNullOrWhiteSpace(configuredUrl) && configuredUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{urlKey} must use https:// in Production.");
            }
        }

        var connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
        if (connectionString.Contains("Password=myPassword", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Password=privateclouddrive", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("ConnectionStrings:Default uses a template/default database password.");
        }

        var passPhrase = configuration["StringEncryption:DefaultPassPhrase"];
        if (string.IsNullOrWhiteSpace(passPhrase) ||
            string.Equals(passPhrase, "NWdpATI5trUHk4X2", StringComparison.Ordinal) ||
            string.Equals(passPhrase, "change-this-32-character-secret", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("StringEncryption:DefaultPassPhrase must be replaced with a deployment secret.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "PrivateCloudDrive refused to start with insecure Production settings: " + string.Join(" ", failures));
        }
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"PrivateCloudDrive", "PrivateCloudDrive API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "PrivateCloudDrive API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    /// <summary>
    /// 响应框架生命周期或界面事件，并协调页面状态与业务操作。
    /// </summary>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
            app.Use(async (httpContext, next) =>
            {
                if (IsApiRequest(httpContext.Request.Path))
                {
                    var statusCodePagesFeature = httpContext.Features.Get<IStatusCodePagesFeature>();
                    if (statusCodePagesFeature != null)
                    {
                        statusCodePagesFeature.Enabled = false;
                    }
                }

                await next();
            });
        }

        app.UseCorrelationId();
        app.MapAbpStaticAssets();
        app.UseRouting();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        if (IsSwaggerEnabled(context.ServiceProvider.GetRequiredService<IConfiguration>(), env))
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrivateCloudDrive API");

                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                c.OAuthScopes("PrivateCloudDrive");
            });
        }

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
