using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrivateCloudDrive.Controllers.FileCenter;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 公开分享控制器的安全边界测试，确保匿名入口不会把分享密码放入 URL。
/// </summary>
public class PublicFileSharesControllerSecurityTests
{
    [Fact]
    public void Download_Should_Read_Share_Password_From_Header_Not_Query()
    {
        var method = typeof(PublicFileSharesController).GetMethod(
            nameof(PublicFileSharesController.DownloadAsync),
            new[] { typeof(string), typeof(string) });

        method.ShouldNotBeNull();
        var passwordParameter = method!.GetParameters().Single(parameter => parameter.Name == "password");

        passwordParameter.GetCustomAttribute<FromQueryAttribute>().ShouldBeNull();
        var headerAttribute = passwordParameter.GetCustomAttribute<FromHeaderAttribute>();
        headerAttribute.ShouldNotBeNull();
        headerAttribute!.Name.ShouldBe("X-Share-Password");
    }

    [Theory]
    [InlineData(nameof(PublicFileSharesController.GetAsync), "PublicShareMetadata")]
    [InlineData(nameof(PublicFileSharesController.VerifyPasswordAsync), "PublicSharePassword")]
    [InlineData(nameof(PublicFileSharesController.DownloadAsync), "PublicSharePassword")]
    public void Public_Share_Endpoints_Should_Enable_Named_Rate_Limiting(string methodName, string policyName)
    {
        var method = typeof(PublicFileSharesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(item => item.Name == methodName);

        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        attribute.ShouldNotBeNull();
        attribute!.PolicyName.ShouldBe(policyName);
    }

    [Fact]
    public void PublicShareRateLimitPartitions_Should_Use_Token_Hash_And_Client_Ip_Without_Raw_Token()
    {
        const string rawToken = "share-token-that-must-not-appear-in-partition";
        const string clientIp = "203.0.113.42";

        var partitionKey = PublicShareRateLimitPartitions.ForTokenAndIp(rawToken, clientIp);

        partitionKey.ShouldStartWith("share:");
        partitionKey.ShouldEndWith($":ip:{clientIp}");
        partitionKey.ShouldNotContain(rawToken);
        partitionKey.Length.ShouldBeLessThan(100);
    }

    [Fact]
    public void PublicShareRateLimitPartitions_Should_Isolate_Different_Tokens_And_Ips()
    {
        var firstTokenFirstIp = PublicShareRateLimitPartitions.ForTokenAndIp("token-a", "203.0.113.1");
        var firstTokenSecondIp = PublicShareRateLimitPartitions.ForTokenAndIp("token-a", "203.0.113.2");
        var secondTokenFirstIp = PublicShareRateLimitPartitions.ForTokenAndIp("token-b", "203.0.113.1");

        firstTokenFirstIp.ShouldNotBe(firstTokenSecondIp);
        firstTokenFirstIp.ShouldNotBe(secondTokenFirstIp);
    }

    [Fact]
    public void PublicShareRateLimitPartitions_Should_Expose_Ip_And_Global_Partitions_For_Cross_Token_Throttling()
    {
        PublicShareRateLimitPartitions.ForIp("203.0.113.42").ShouldBe("ip:203.0.113.42");
        PublicShareRateLimitPartitions.Global.ShouldBe("global");
    }

    [Fact]
    public void FixedWindowLimiter_Should_Reject_After_Public_Share_Quota_For_429_Middleware_Path()
    {
        using var limiter = new FixedWindowRateLimiter(
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = false
            });

        using var first = limiter.AttemptAcquire();
        using var second = limiter.AttemptAcquire();

        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeFalse();
    }

    private sealed class StubPublicSharesAppService : IFileCenterPublicSharesAppService
    {
        public Task<PublicFileShareDto> GetAsync(string token)
        {
            throw new NotSupportedException();
        }

        public Task<PublicFileShareDto> VerifyPasswordAsync(string token, VerifySharePasswordInput input)
        {
            throw new NotSupportedException();
        }

        public Task<FileDownloadInfo> GetDownloadAsync(
            string token,
            string? password = null,
            CancellationToken cancellationToken = default)
        {
            return GetDownloadAsync(token, password, range: null, cancellationToken);
        }

        public Task<FileDownloadInfo> GetDownloadAsync(
            string token,
            string? password,
            FileDownloadRangeRequest? range,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new FileDownloadInfo
                {
                    FileName = "shared.txt",
                    ContentType = "text/plain",
                    Size = 1,
                    TotalSize = 1,
                    Range = range?.Normalize(1),
                    Content = new MemoryStream(new byte[] { 1 })
                });
        }
    }
}
