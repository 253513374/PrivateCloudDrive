using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    [InlineData(nameof(PublicFileSharesController.VerifyPasswordAsync))]
    [InlineData(nameof(PublicFileSharesController.DownloadAsync))]
    public void Password_Protected_Public_Share_Endpoints_Should_Enable_Rate_Limiting(string methodName)
    {
        var method = typeof(PublicFileSharesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(item => item.Name == methodName);

        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        attribute.ShouldNotBeNull();
        attribute!.PolicyName.ShouldBe("PublicSharePassword");
    }

    [Fact]
    public void Public_Share_AppService_Should_Not_Be_Exposed_As_Conventional_Controller()
    {
        var attribute = typeof(IFileCenterPublicSharesAppService)
            .GetCustomAttribute<RemoteServiceAttribute>();

        attribute.ShouldNotBeNull();
        attribute!.IsEnabled.ShouldBeFalse();
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
