using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 验证 FileCenter 存储 Provider 配置解析和阿里云 OSS 必填项校验。
/// </summary>
public class FileCenterStorageProviderOptionsTests
{
    /// <summary>
    /// 验证未显式配置时默认使用本地文件系统存储。
    /// </summary>
    [Fact]
    public void Should_Default_To_FileSystem()
    {
        FileCenterStorageProviderNames.Normalize(null)
            .ShouldBe(FileCenterStorageProviderNames.FileSystem);
    }

    /// <summary>
    /// 验证已知 Provider 名称可以被大小写不敏感地标准化。
    /// </summary>
    [Theory]
    [InlineData("FileSystem", FileCenterStorageProviderNames.FileSystem)]
    [InlineData("filesystem", FileCenterStorageProviderNames.FileSystem)]
    [InlineData("AliyunOss", FileCenterStorageProviderNames.AliyunOss)]
    [InlineData("aliyunoss", FileCenterStorageProviderNames.AliyunOss)]
    public void Should_Normalize_Known_Storage_Providers(string input, string expected)
    {
        FileCenterStorageProviderNames.Normalize(input).ShouldBe(expected);
    }

    /// <summary>
    /// 验证未知 Provider 名称会被拒绝，避免启动后进入不明确状态。
    /// </summary>
    [Fact]
    public void Should_Reject_Unsupported_Storage_Provider()
    {
        Should.Throw<InvalidOperationException>(() =>
            FileCenterStorageProviderNames.Normalize("S3"));
    }

    /// <summary>
    /// 验证可以从配置中读取完整的阿里云 OSS 参数。
    /// </summary>
    [Fact]
    public void Should_Read_AliyunOss_Options()
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["FileCenter:AliyunOss:AccessKeyId"] = "access-key",
                ["FileCenter:AliyunOss:AccessKeySecret"] = "secret",
                ["FileCenter:AliyunOss:Endpoint"] = "oss-cn-hangzhou.aliyuncs.com",
                ["FileCenter:AliyunOss:RegionId"] = "cn-hangzhou",
                ["FileCenter:AliyunOss:BucketName"] = "privateclouddrive",
                ["FileCenter:AliyunOss:CreateBucketIfNotExists"] = "true"
            });

        var options = FileCenterAliyunOssOptions.FromConfiguration(configuration);

        options.AccessKeyId.ShouldBe("access-key");
        options.AccessKeySecret.ShouldBe("secret");
        options.Endpoint.ShouldBe("oss-cn-hangzhou.aliyuncs.com");
        options.RegionId.ShouldBe("cn-hangzhou");
        options.BucketName.ShouldBe("privateclouddrive");
        options.CreateBucketIfNotExists.ShouldBeTrue();
    }

    /// <summary>
    /// 验证启用阿里云 OSS 时缺少必填项会抛出明确异常。
    /// </summary>
    [Fact]
    public void Should_Reject_Missing_AliyunOss_Required_Options()
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["FileCenter:AliyunOss:AccessKeyId"] = "access-key"
            });

        Should.Throw<InvalidOperationException>(() =>
            FileCenterAliyunOssOptions.FromConfiguration(configuration));
    }

    /// <summary>
    /// 构造用于配置解析测试的内存配置。
    /// </summary>
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
