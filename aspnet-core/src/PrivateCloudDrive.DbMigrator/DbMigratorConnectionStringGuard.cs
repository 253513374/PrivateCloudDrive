using System;
using Npgsql;

namespace PrivateCloudDrive.DbMigrator;

/// <summary>
/// 在执行迁移前校验连接字符串形态，避免被 Docker 链接变量等环境污染后把 URI 当作主机名传给 Npgsql。
/// </summary>
public static class DbMigratorConnectionStringGuard
{
    /// <summary>
    /// 校验默认连接字符串，不输出密码、Token 或完整连接串。
    /// </summary>
    public static void Validate(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is required for DbMigrator.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("ConnectionStrings:Default is invalid for DbMigrator. Check host, port and database settings; secret values are hidden.", exception);
        }

        var host = builder.Host?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("ConnectionStrings:Default Host is required for DbMigrator.");
        }

        if (host.Contains("://", StringComparison.Ordinal) || Uri.TryCreate(host, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("ConnectionStrings:Default Host must be a DNS name or IP address, not a URI. Use Host=postgres;Port=5432 for the local Docker stack.");
        }
    }
}
