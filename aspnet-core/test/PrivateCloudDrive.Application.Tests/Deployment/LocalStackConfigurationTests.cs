using System;
using System.IO;
using PrivateCloudDrive.DbMigrator;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.Deployment;

/// <summary>
/// 覆盖本地 Docker 栈验证脚本依赖的配置契约，防止 Compose 默认值再次阻断非 Preflight 验收。
/// </summary>
public class LocalStackConfigurationTests
{
    [Fact]
    public void Docker_Compose_Should_Enable_Local_Validation_Defaults()
    {
        var compose = File.ReadAllText(GetRepositoryFile("docker-compose.yml"));

        compose.ShouldContain("AuthServer__RequireHttpsMetadata: ${AUTH_SERVER_REQUIRE_HTTPS_METADATA:-false}");
        compose.ShouldContain("Swagger__Enabled: ${SWAGGER_ENABLED:-true}");
        compose.ShouldContain("Security__AllowInsecureTransportForLocalValidation: ${ALLOW_INSECURE_LOCAL_VALIDATION:-true}");
    }

    [Fact]
    public void DbMigrator_Should_Reject_Uri_Scheme_In_Connection_Host()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            DbMigratorConnectionStringGuard.Validate("Host=tcp://postgres:5432;Port=5432;Database=PrivateCloudDrive;Username=privateclouddrive;Password=hidden;"));

        exception.Message.ShouldContain("ConnectionStrings:Default Host must be a DNS name or IP address");
        exception.Message.ShouldNotContain("hidden");
    }

    private static string GetRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file '{relativePath}' from '{AppContext.BaseDirectory}'.");
    }
}
