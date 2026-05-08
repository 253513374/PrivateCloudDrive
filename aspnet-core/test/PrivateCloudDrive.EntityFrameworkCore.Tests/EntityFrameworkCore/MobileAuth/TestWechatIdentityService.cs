using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.MobileAuth;

public class TestWechatIdentityService :
    IWechatIdentityService,
    ITransientDependency
{
    public const string AppId = "wx-test";
    public const string AliceCode = "wechat-alice";
    public const string AliceUpdatedCode = "wechat-alice-updated";
    public const string BobCode = "wechat-bob";
    public const string FailedCode = "wechat-failed";

    public Task<WechatIdentity> ExchangeAsync(
        string code,
        string? platform = null,
        CancellationToken cancellationToken = default)
    {
        if (code == FailedCode)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.WeChatCodeExchangeFailed)
                .WithData("error", WechatLoginConsts.CodeExchangeFailedError);
        }

        return Task.FromResult(code switch
        {
            AliceUpdatedCode => CreateAlice("Alice Updated"),
            AliceCode => CreateAlice("Alice"),
            BobCode => new WechatIdentity
            {
                AppId = AppId,
                OpenId = "openid-bob",
                UnionId = "union-bob",
                NickName = "Bob",
                AvatarUrl = "https://example.test/bob.png"
            },
            _ => new WechatIdentity
            {
                AppId = AppId,
                OpenId = $"openid-{code}",
                UnionId = $"union-{code}",
                NickName = code,
                AvatarUrl = null
            }
        });
    }

    private static WechatIdentity CreateAlice(string nickName)
    {
        return new WechatIdentity
        {
            AppId = AppId,
            OpenId = "openid-alice",
            UnionId = "union-alice",
            NickName = nickName,
            AvatarUrl = "https://example.test/alice.png"
        };
    }
}
