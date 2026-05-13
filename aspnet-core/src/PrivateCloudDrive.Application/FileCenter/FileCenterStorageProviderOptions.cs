using System;
using Microsoft.Extensions.Configuration;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 定义 FileCenter 支持的 Blob 存储 Provider 名称，并提供配置值标准化能力。
/// </summary>
public static class FileCenterStorageProviderNames
{
    /// <summary>
    /// 表示本地文件系统 Blob 存储 Provider。
    /// </summary>
    public const string FileSystem = "FileSystem";

    /// <summary>
    /// 表示阿里云 OSS Blob 存储 Provider。
    /// </summary>
    public const string AliyunOss = "AliyunOss";

    /// <summary>
    /// 将配置中的 Provider 名称标准化为系统支持的固定值。
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FileSystem;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Equals(FileSystem, StringComparison.OrdinalIgnoreCase))
        {
            return FileSystem;
        }

        if (normalizedValue.Equals(AliyunOss, StringComparison.OrdinalIgnoreCase))
        {
            return AliyunOss;
        }

        throw new InvalidOperationException(
            $"Unsupported FileCenter storage provider '{value}'. Supported values are '{FileSystem}' and '{AliyunOss}'.");
    }
}

/// <summary>
/// 表示 FileCenter 使用阿里云 OSS 时所需的后端配置。
/// </summary>
public sealed class FileCenterAliyunOssOptions
{
    /// <summary>
    /// 获取阿里云 RAM AccessKey ID。
    /// </summary>
    public string AccessKeyId { get; private init; } = null!;

    /// <summary>
    /// 获取阿里云 RAM AccessKey Secret。
    /// </summary>
    public string AccessKeySecret { get; private init; } = null!;

    /// <summary>
    /// 获取 OSS Endpoint，例如 oss-cn-hangzhou.aliyuncs.com。
    /// </summary>
    public string Endpoint { get; private init; } = null!;

    /// <summary>
    /// 获取 OSS 所在区域 ID，例如 cn-hangzhou。
    /// </summary>
    public string RegionId { get; private init; } = null!;

    /// <summary>
    /// 获取 FileCenter 使用的私有 OSS Bucket 名称。
    /// </summary>
    public string BucketName { get; private init; } = null!;

    /// <summary>
    /// 获取是否允许 Provider 在 Bucket 不存在时自动创建 Bucket。
    /// </summary>
    public bool CreateBucketIfNotExists { get; private init; }

    /// <summary>
    /// 从应用配置中读取并校验阿里云 OSS 必填项。
    /// </summary>
    public static FileCenterAliyunOssOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("FileCenter:AliyunOss");

        return new FileCenterAliyunOssOptions
        {
            AccessKeyId = GetRequired(section, "AccessKeyId"),
            AccessKeySecret = GetRequired(section, "AccessKeySecret"),
            Endpoint = GetRequired(section, "Endpoint"),
            RegionId = GetRequired(section, "RegionId"),
            BucketName = GetRequired(section, "BucketName"),
            CreateBucketIfNotExists = bool.TryParse(section["CreateBucketIfNotExists"], out var createBucket) &&
                                      createBucket
        };
    }

    /// <summary>
    /// 读取必填配置项，缺失时抛出明确的启动异常。
    /// </summary>
    private static string GetRequired(IConfiguration section, string key)
    {
        var value = section[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException(
            $"FileCenter:AliyunOss:{key} is required when FileCenter:StorageProvider is '{FileCenterStorageProviderNames.AliyunOss}'.");
    }
}
