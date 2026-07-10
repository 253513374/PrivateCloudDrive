using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrivateCloudDrive.FileCenter;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.OpenIddict.Applications;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 部署健康检查应用服务，为运维人员提供分层的部署后系统就绪确认。
/// live → 仅进程存活；ready → 低敏依赖就绪；detail → 管理员全量详情。
/// 所有输出均不包含密码、token、OAuth code、client secret 或完整私有 URL。
/// </summary>
public class DeploymentHealthCheckService : IDeploymentHealthCheckService, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache<DeploymentHealthCacheItem, string> _cache;
    private readonly FileCenterMediaProcessingOptions _mediaProcessingOptions;
    private readonly IOpenIddictApplicationRepository _openIddictApplicationRepository;
    private readonly ILogger<DeploymentHealthCheckService> _logger;

    private static readonly HashSet<string> ForbiddenSensitiveMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password=", "myPassword", "privateclouddrive",
        "client_secret", "client secret",
        "AccessKeyId", "AccessKeySecret", "DefaultPassPhrase",
        "raw exception",
    };

    // 正则模式集：用于增强脱敏
    private static readonly Regex WindowsDrivePathPattern = new(
        @"[A-Za-z]:\\[^\s,:;""<>|?*]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LinuxAbsolutePathPattern = new(
        @"(?:/home/[^\s,:;""<>]+|/var/[^\s,:;""<>]+|/etc/[^\s,:;""<>]+|/opt/[^\s,:;""<>]+|/usr/[^\s,:;""<>]+|/tmp/[^\s,:;""<>]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectionStringPasswordPattern = new(
        @"((?:Password|Pwd|Passwd)\s*=\s*)(?:(?!\s|;|""|$)[^\s;""])+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JwtTokenPattern = new(
        @"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}",
        RegexOptions.Compiled);

    private static readonly Regex AccessKeySecretPattern = new(
        @"(AccessKey(?:Secret|Id)?\s*[:=]\s*)(?:(?!\s|;|""|,|$)[^\s;"",])+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>脱敏 AccessKey 值（不含 Secret/Id 标记时的泛化模式）。</summary>
    private static readonly Regex GenericAccessKeyPattern = new(
        @"(?:AccessKey|AccessSecret)\s*[:=]\s*(?:(?!\s|;|""|,|$)[^\s;"",])+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 初始化 <see cref="DeploymentHealthCheckService"/> 的新实例。
    /// </summary>
    public DeploymentHealthCheckService(
        IConfiguration configuration,
        IDistributedCache<DeploymentHealthCacheItem, string> cache,
        IOptions<FileCenterMediaProcessingOptions> mediaProcessingOptions,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        ILogger<DeploymentHealthCheckService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _mediaProcessingOptions = mediaProcessingOptions.Value;
        _openIddictApplicationRepository = openIddictApplicationRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task<DeploymentHealthDto> GetHealthAsync()
    {
        var checks = await RunAllChecksAsync();

        return new DeploymentHealthDto
        {
            OverallStatus = ResolveOverallStatus(checks),
            Checks = checks,
            GeneratedAt = DateTime.UtcNow,
            ContainsSensitiveData = false
        };
    }

    /// <inheritdoc/>
    public virtual Task<DeploymentLiveDto> GetLiveAsync()
    {
        return Task.FromResult(new DeploymentLiveDto
        {
            Status = "Healthy",
            GeneratedAt = DateTime.UtcNow
        });
    }

    /// <inheritdoc/>
    public virtual async Task<DeploymentReadyDto> GetReadyAsync()
    {
        var checks = await RunAllChecksAsync();

        return new DeploymentReadyDto
        {
            OverallStatus = ResolveOverallStatus(checks),
            Checks = checks.Select(c => new DeploymentReadyCheckDto
            {
                Name = c.Name,
                Status = c.Status,
                // ready 层只返回低敏消息：不含修复建议、物理路径、连接串详情
                Message = ResolveReadyMessage(c)
            }).ToList(),
            GeneratedAt = DateTime.UtcNow,
            ContainsSensitiveData = false
        };
    }

    /// <summary>
    /// 运行所有检查项，返回完整的 DeploymentHealthDto 级别结果。
    /// </summary>
    private async Task<List<DeploymentCheckResultDto>> RunAllChecksAsync()
    {
        var checks = new List<DeploymentCheckResultDto>();

        checks.Add(await CheckDatabaseConnectionAsync());
        checks.Add(await CheckRedisConnectionAsync());
        checks.Add(CheckStorageWritability());
        checks.Add(CheckToolAvailability("FFmpeg", _mediaProcessingOptions.FfmpegPath));
        checks.Add(CheckToolAvailability("FFprobe", _mediaProcessingOptions.FfprobePath));
        checks.Add(CheckOpenIddictIssuerUrl());
        checks.Add(await CheckOpenIddictRedirectUrlsAsync());
        checks.Add(CheckProductionSecuritySettings());

        return checks;
    }

    /// <summary>
    /// 为 ready 层生成低敏消息：仅指示组件是否可用，不含修复建议或内部路径。
    /// </summary>
    private static string ResolveReadyMessage(DeploymentCheckResultDto check)
    {
        return check.Status switch
        {
            DeploymentCheckStatus.Pass => $"{check.Name} 正常",
            DeploymentCheckStatus.Warn => $"{check.Name} 降级",
            DeploymentCheckStatus.Fail => $"{check.Name} 不可用",
            _ => $"{check.Name} 状态未知"
        };
    }

    private static DeploymentCheckStatus ResolveOverallStatus(IReadOnlyList<DeploymentCheckResultDto> checks)
    {
        if (checks.Any(c => c.Status == DeploymentCheckStatus.Fail))
        {
            return DeploymentCheckStatus.Fail;
        }

        return checks.Any(c => c.Status == DeploymentCheckStatus.Warn)
            ? DeploymentCheckStatus.Warn
            : DeploymentCheckStatus.Pass;
    }

    /// <summary>
    /// 检查数据库连接是否可用。使用配置的连接字符串尝试打开连接。
    /// </summary>
    private async Task<DeploymentCheckResultDto> CheckDatabaseConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return FailResult(
                "数据库连接",
                "未配置数据库连接字符串。",
                "请在 ConnectionStrings:Default 中配置 PostgreSQL 连接字符串，例如：Host=localhost;Port=5432;Database=<database-name>;User ID=root;Password=...");
        }

        try
        {
            // 使用 DbProviderFactories 获取已注册的 PostgreSQL 提供程序工厂
            // 注：Application 层不直接依赖 Npgsql，使用系统注册的提供程序
            var factory = DbProviderFactories.GetFactory("Npgsql");
            await using var connection = factory.CreateConnection();
            if (connection == null)
            {
                return FailResult("数据库连接", "无法创建数据库连接实例。", "请检查 Npgsql 依赖及 PostgreSQL 服务状态。");
            }

            connection.ConnectionString = connectionString;
            await connection.OpenAsync();

            // 额外验证：执行简单查询确认数据库可读写
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();

            return PassResult("数据库连接", "PostgreSQL 连接正常，数据库可读写。");
        }
        catch (ArgumentException ex)
        {
            // 测试环境下 Npgsql 可能未注册（使用 SQLite），返回 Warn 而非 Fail
            _logger.LogInformation(ex, "Npgsql 提供程序未注册，可能是测试/开发环境使用 SQLite");
            return WarnResult(
                "数据库连接",
                "当前环境无法使用 Npgsql/PostgreSQL 提供程序验证数据库连接。请确认数据库服务可访问。",
                "生产环境请确保已安装 Npgsql 依赖。测试/开发环境使用 SQLite 则可忽略此警告。");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "数据库连接检查失败");
            return FailResult(
                "数据库连接",
                $"数据库连接失败：{SanitizeErrorMessage(ex.Message)}",
                "请确认 PostgreSQL 服务是否运行，连接字符串中的 Host/Port/User ID/Password 是否正确，以及防火墙是否放行。");
        }
    }

    /// <summary>
    /// 检查 Redis/分布式缓存连接是否可用。使用写入后读取探针验证缓存链路。
    /// </summary>
    private async Task<DeploymentCheckResultDto> CheckRedisConnectionAsync()
    {
        try
        {
            var probeId = $"deploy-health:{Guid.NewGuid():N}";
            var item = new DeploymentHealthCacheItem { ProbeId = probeId };

            await _cache.SetAsync(probeId, item, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

            var restored = await _cache.GetAsync(probeId);
            await _cache.RemoveAsync(probeId);

            if (restored?.ProbeId != probeId)
            {
                return WarnResult(
                    "Redis/分布式缓存",
                    "分布式缓存探针读写不一致，缓存可能使用内存后端而非 Redis。",
                    "如已配置 Redis 请检查连接字符串和 Redis 服务状态。如果使用内存缓存作为开发环境则可忽略此警告。");
            }

            return PassResult("Redis/分布式缓存", "分布式缓存连接正常，读写探针验证通过。");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis 连接检查失败");
            return FailResult(
                "Redis/分布式缓存",
                $"分布式缓存连接失败：{SanitizeErrorMessage(ex.Message)}",
                "请确认 Redis 服务是否运行，Abp:DistributedCache:KeyPrefix 和 Redis 配置是否正确，以及网络防火墙是否放行。");
        }
    }

    /// <summary>
    /// 检查文件存储卷是否可读写。尝试写入探针文件、读取验证、然后删除。
    /// </summary>
    private DeploymentCheckResultDto CheckStorageWritability()
    {
        try
        {
            var storageRootPath = FileCenterBlobStoragePath.GetFullPath(_configuration);
            if (string.IsNullOrWhiteSpace(storageRootPath))
            {
                return FailResult(
                    "存储卷可写性",
                    "文件存储根路径未配置。",
                    "请在 FileCenter:StorageRootPath 中配置文件存储路径（默认 App_Data/FileCenter）。");
            }

            // 确保存储目录存在
            Directory.CreateDirectory(storageRootPath);

            var probeFileName = $".health-probe-{Guid.NewGuid():N}.tmp";
            var probeFilePath = Path.Combine(storageRootPath, probeFileName);
            var probeContent = $"PrivateCloudDrive health probe: {DateTime.UtcNow:O}";

            try
            {
                // 写入探针文件
                File.WriteAllText(probeFilePath, probeContent);

                // 读取验证
                var readBack = File.ReadAllText(probeFilePath);
                if (readBack != probeContent)
                {
                    return FailResult(
                        "存储卷可写性",
                        "存储卷写入后读取内容不一致，存储可能存在只读或权限问题。",
                        "请确认存储路径的读写权限，检查磁盘是否已满或文件系统是否以只读方式挂载。");
                }

                return PassResult(
                    "存储卷可写性",
                    $"存储卷可读写（路径：{SanitizePath(storageRootPath)}），探针文件写入/读取/删除验证通过。");
            }
            finally
            {
                // 清理探针文件
                try
                {
                    if (File.Exists(probeFilePath))
                    {
                        File.Delete(probeFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理存储探针文件失败：{ProbePath}", probeFilePath);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return FailResult(
                "存储卷可写性",
                "无权限写入存储目录。",
                "请确认应用运行用户对存储路径具有读写权限。");
        }
        catch (IOException ex) when (ex.Message.Contains("disk full", StringComparison.OrdinalIgnoreCase) ||
                                      ex.Message.Contains("no space", StringComparison.OrdinalIgnoreCase))
        {
            return FailResult(
                "存储卷可写性",
                "磁盘空间不足，无法写入探针文件。",
                "请释放磁盘空间或扩展存储卷。");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "存储卷可写性检查失败");
            return FailResult(
                "存储卷可写性",
                $"存储卷检查失败：{SanitizeErrorMessage(ex.Message)}",
                "请确认存储路径配置正确且文件系统可写。");
        }
    }

    /// <summary>
    /// 检查 FFmpeg 或 FFprobe 工具是否可执行。
    /// </summary>
    private DeploymentCheckResultDto CheckToolAvailability(string displayName, string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return WarnResult(
                displayName,
                $"{displayName} 未配置路径。",
                $"请在 FileCenter:MediaProcessing:{displayName}Path 中配置 {displayName} 可执行文件路径。如果不需要媒体处理功能可忽略此警告。");
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return FailResult(
                    displayName,
                    $"无法启动 {displayName} 进程。",
                    $"请确认 {displayName} 可执行文件路径是否有效，是否已安装 {displayName}。");
            }

            // 等待退出并获取输出来验证
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(10));

            if (process.ExitCode != 0)
            {
                return FailResult(
                    displayName,
                    $"{displayName} 执行失败（退出码：{process.ExitCode}）。",
                    $"请检查 {displayName} 安装完整性，尝试在命令行中执行确认。");
            }

            return PassResult(displayName, $"{displayName} 可执行，版本信息：{TruncateVersion(output ?? error)}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Tool} 检查失败", displayName);
            return FailResult(
                displayName,
                $"{displayName} 检查失败：{SanitizeErrorMessage(ex.Message)}",
                $"请确保已安装 {displayName}，且路径配置正确。");
        }
    }

    /// <summary>
    /// 检查 OpenIddict Issuer URL 配置是否有效。
    /// </summary>
    private DeploymentCheckResultDto CheckOpenIddictIssuerUrl()
    {
        var authority = _configuration["AuthServer:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
        {
            return FailResult(
                "OpenIddict Issuer URL",
                "AuthServer:Authority 未配置。",
                "请在配置中设置 AuthServer:Authority 为完整的 Issuer URL（例如 https://your-domain.com）。");
        }

        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
        {
            return FailResult(
                "OpenIddict Issuer URL",
                $"AuthServer:Authority 配置值「{SanitizeUrlForDisplay(authority)}」不是有效的绝对 URL。",
                "请确保 AuthServer:Authority 配置为有效的 HTTPS URL，例如 https://your-domain.com。");
        }

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            var requireHttps = _configuration.GetValue("AuthServer:RequireHttpsMetadata", true);
            if (requireHttps)
            {
                return FailResult(
                    "OpenIddict Issuer URL",
                    $"AuthServer:Authority 使用 http 协议，但 RequireHttpsMetadata 为 true。生产环境必须使用 HTTPS。",
                    "在配置中使用 https:// 协议，或将 RequireHttpsMetadata 设为 false（仅开发环境）。");
            }

            return WarnResult(
                "OpenIddict Issuer URL",
                $"AuthServer:Authority 使用 http 协议（RequireHttpsMetadata 已禁用）。生产环境建议使用 HTTPS。",
                "生产环境请配置有效的 HTTPS 证书并启用 RequireHttpsMetadata。");
        }

        return PassResult("OpenIddict Issuer URL", $"AuthServer:Authority 已配置为有效的 HTTPS URL（{uri.Host}）。");
    }

    /// <summary>
    /// 检查 OpenIddict 已注册应用是否配置了有效的 Redirect URLs。
    /// </summary>
    private async Task<DeploymentCheckResultDto> CheckOpenIddictRedirectUrlsAsync()
    {
        try
        {
            var applications = await _openIddictApplicationRepository.GetListAsync();
            if (applications.Count == 0)
            {
                return WarnResult(
                    "OpenIddict 应用 Redirect URLs",
                    "尚未注册任何 OpenIddict 客户端应用。数据库迁移后首次运行时会自动创建。",
                    "如果系统尚未初始化，启动 DbMigrator 工具执行种子数据创建即可。");
            }

            var appsWithoutRedirect = new List<string>();
            var appsWithoutValidUrls = new List<string>();

            foreach (var app in applications)
            {
                var redirectUris = app.RedirectUris;
                if (string.IsNullOrWhiteSpace(redirectUris))
                {
                    if (app.ClientType == "public" || app.ClientType == "confidential")
                    {
                        appsWithoutRedirect.Add(app.ClientId);
                    }

                    continue;
                }

                // 检查所有配置的 Redirect URI 是否有效
                var uris = System.Text.Json.JsonSerializer.Deserialize<List<string>>(redirectUris) ?? [];
                var hasAnyValidUri = uris.Any(u => Uri.TryCreate(u, UriKind.Absolute, out var parsed) && parsed.IsWellFormedOriginalString());
                if (!hasAnyValidUri && uris.Count > 0)
                {
                    appsWithoutValidUrls.Add(app.ClientId);
                }
            }

            if (appsWithoutRedirect.Count > 0)
            {
                return WarnResult(
                    "OpenIddict 应用 Redirect URLs",
                    $"以下应用未配置 Redirect URLs：{string.Join("、", appsWithoutRedirect.Select(SanitizeClientId))}。部分授权流程可能受限。",
                    "在 OpenIddict:Applications 配置节中为这些应用配置 RedirectUri/RedirectUris。");
            }

            if (appsWithoutValidUrls.Count > 0)
            {
                return WarnResult(
                    "OpenIddict 应用 Redirect URLs",
                    $"以下应用的 Redirect URLs 中存在无效格式：{string.Join("、", appsWithoutValidUrls.Select(SanitizeClientId))}。",
                    "检查这些应用的 RedirectUris 配置项，确保每个 URL 都是有效的绝对 URL。");
            }

            return PassResult("OpenIddict 应用 Redirect URLs", $"共 {applications.Count} 个应用已注册，Redirect URLs 验证通过。");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenIddict 应用 RedirectURLs 检查失败");
            return WarnResult(
                "OpenIddict 应用 Redirect URLs",
                $"检查失败：{SanitizeErrorMessage(ex.Message)}。",
                "如果数据库还未初始化或没有 OpenIddict 表，此警告可忽略。运行 DbMigrator 初始化数据库即可。");
        }
    }

    /// <summary>
    /// 检查生产环境安全配置（HTTPS、默认密码检测）。
    /// </summary>
    private DeploymentCheckResultDto CheckProductionSecuritySettings()
    {
        var warnings = new List<string>();
        var failures = new List<string>();

        // 检查 HTTPS 配置
        var selfUrl = _configuration["App:SelfUrl"];
        if (!string.IsNullOrWhiteSpace(selfUrl) && selfUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("App:SelfUrl 使用 http 协议，生产环境建议使用 https。");
        }

        var authority = _configuration["AuthServer:Authority"];
        if (!string.IsNullOrWhiteSpace(authority) && authority.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            var requireHttps = _configuration.GetValue("AuthServer:RequireHttpsMetadata", true);
            if (requireHttps)
            {
                failures.Add("AuthServer:Authority 使用 http，但 RequireHttpsMetadata=true。");
            }
            else
            {
                warnings.Add("AuthServer:Authority 使用 http（RequireHttpsMetadata 已禁用）。");
            }
        }

        // 检查默认密码
        var connectionString = _configuration.GetConnectionString("Default") ?? string.Empty;
        if (connectionString.Contains("Password=myPassword", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Password=privateclouddrive", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("数据库连接字符串使用了默认/模板密码。");
        }

        // 检查默认加密密钥
        var passPhrase = _configuration["StringEncryption:DefaultPassPhrase"];
        if (string.IsNullOrWhiteSpace(passPhrase))
        {
            failures.Add("StringEncryption:DefaultPassPhrase 未配置。");
        }
        else if (string.Equals(passPhrase, "NWdpATI5trUHk4X2", StringComparison.Ordinal) ||
                 string.Equals(passPhrase, "change-this-32-character-secret", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("StringEncryption:DefaultPassPhrase 使用了默认值，必须替换为部署密钥。");
        }

        // 检查 Swagger 在生产环境是否意外开启
        var swaggerEnabled = _configuration.GetValue("Swagger:Enabled", false);
        if (swaggerEnabled)
        {
            var envName = _configuration["ASPNETCORE_ENVIRONMENT"] ?? string.Empty;
            if (!string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Swagger 在非 Development 环境中已开启，存在 API 文档和端点泄露风险。请将 Swagger:Enabled 设为 false 或仅在 Development 环境启用。");
            }
        }

        // 组合结果
        if (failures.Count > 0 && warnings.Count > 0)
        {
            return WarnResult(
                "安全配置检查",
                $"发现 {failures.Count} 项安全风险、{warnings.Count} 项警告。",
                string.Join(" ", failures.Concat(warnings).Select(w => $"🔴 {w}")));
        }

        if (failures.Count > 0)
        {
            return WarnResult(
                "安全配置检查",
                $"发现 {failures.Count} 项安全风险，建议在部署前处理。",
                string.Join(" ", failures.Select(f => $"🔴 {f}")));
        }

        if (warnings.Count > 0)
        {
            return WarnResult(
                "安全配置检查",
                $"发现 {warnings.Count} 项安全警告。",
                string.Join(" ", warnings.Select(w => $"🟡 {w}")));
        }

        return PassResult("安全配置检查", "HTTPS 配置正确，未使用默认密码或密钥。");
    }

    private static DeploymentCheckResultDto PassResult(string name, string message)
    {
        return new DeploymentCheckResultDto
        {
            Name = name,
            Status = DeploymentCheckStatus.Pass,
            Message = message
        };
    }

    private static DeploymentCheckResultDto WarnResult(string name, string message, string? fixSuggestion)
    {
        return new DeploymentCheckResultDto
        {
            Name = name,
            Status = DeploymentCheckStatus.Warn,
            Message = message,
            FixSuggestion = fixSuggestion
        };
    }

    private static DeploymentCheckResultDto FailResult(string name, string message, string? fixSuggestion)
    {
        return new DeploymentCheckResultDto
        {
            Name = name,
            Status = DeploymentCheckStatus.Fail,
            Message = message,
            FixSuggestion = fixSuggestion
        };
    }

    /// <summary>
    /// 脱敏错误消息：删除所有敏感标记、路径、连接串密码、JWT token、AccessKeySecret。
    /// </summary>
    internal static string SanitizeErrorMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        // 检查 ForbiddenSensitiveMarkers
        foreach (var marker in ForbiddenSensitiveMarkers)
        {
            if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return "发生内部错误（详细信息已脱敏）。";
            }
        }

        var sanitized = message;

        // 脱敏 Windows 驱动器绝对路径 (C:\Users\...)
        sanitized = WindowsDrivePathPattern.Replace(sanitized, "**路径已脱敏**");

        // 脱敏 Linux 绝对路径
        sanitized = LinuxAbsolutePathPattern.Replace(sanitized, "**路径已脱敏**");

        // 脱敏连接串密码 (Password=xxx)
        sanitized = ConnectionStringPasswordPattern.Replace(sanitized, "$1**已脱敏**");

        // 脱敏 JWT token
        sanitized = JwtTokenPattern.Replace(sanitized, "**token已脱敏**");

        // 脱敏 AccessKeySecret
        sanitized = AccessKeySecretPattern.Replace(sanitized, "$1**已脱敏**");

        // 脱敏泛化 AccessKey（不含 Secret/Id 后缀）
        sanitized = GenericAccessKeyPattern.Replace(sanitized, "**AccessKey已脱敏**");

        // 截断过长消息，防止意外泄露
        return sanitized.Length > 200 ? sanitized[..200] + "…" : sanitized;
    }

    internal static string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // 统一分隔符
        var normalized = path.Replace('\\', '/').TrimEnd('/');

        // 先脱敏敏感标记
        var sanitized = normalized;
        foreach (var marker in ForbiddenSensitiveMarkers)
        {
            if (sanitized.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return "路径已脱敏";
            }
        }

        // 脱敏完整 Windows/Linux 路径: 仅保留最后两级
        var segments = sanitized
            .Split('/')
            .Select(s => WindowsDrivePathPattern.IsMatch(s)
                ? "…"
                : s)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (segments.Length <= 2)
        {
            return string.Join("/", segments);
        }

        // 脱敏可能的驱动器盘符路径
        var cleaned = segments[^2..];
        return "…/" + string.Join("/", cleaned);
    }

    private static string SanitizeUrlForDisplay(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch
        {
            return url.Length > 50 ? url[..50] + "…" : url;
        }
    }

    private static string SanitizeClientId(string clientId)
    {
        if (ForbiddenSensitiveMarkers.Any(marker => clientId.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return "客户端（已脱敏）";
        }

        return clientId.Length > 40 ? clientId[..40] + "…" : clientId;
    }

    private static string TruncateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "未知";
        }

        // 取第一行，最多 80 字符
        var firstLine = version.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? version;
        return firstLine.Length > 80 ? firstLine[..80] + "…" : firstLine;
    }

}

/// <summary>
/// 部署健康检查缓存探针，仅用于验证分布式缓存读写链路。
/// </summary>
public class DeploymentHealthCacheItem
{
    public string ProbeId { get; set; } = string.Empty;
}
