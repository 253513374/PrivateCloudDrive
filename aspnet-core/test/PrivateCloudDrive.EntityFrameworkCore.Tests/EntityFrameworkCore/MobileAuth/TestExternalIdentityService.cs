using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace PrivateCloudDrive.MobileAuth;

/// <summary>
/// 提供TestExternalIdentity服务能力，封装可复用的业务或基础设施逻辑。
/// </summary>
public class TestExternalIdentityService :
    IExternalIdentityService,
    ITransientDependency
{
    public const string GoogleAliceCode = "google-alice";
    public const string GoogleAliceUpdatedCode = "google-alice-updated";
    public const string GitHubBobCode = "github-bob";
    public const string FailedCode = "external-failed";

    /// <summary>
    /// 使用授权凭据换取外部身份信息，并避免在日志或返回值中暴露敏感数据。
    /// </summary>
    public Task<ExternalIdentity> ExchangeAsync(
        string provider,
        string code,
        string redirectUri,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = ExternalLoginConsts.NormalizeProvider(provider);
        if (normalizedProvider == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginProviderUnsupported)
                .WithData("error", ExternalLoginConsts.ProviderUnsupportedError);
        }

        if (code == FailedCode)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.ExternalLoginCodeExchangeFailed)
                .WithData("error", ExternalLoginConsts.CodeExchangeFailedError);
        }

        return Task.FromResult((normalizedProvider, code) switch
        {
            (ExternalLoginConsts.GoogleProviderName, GoogleAliceUpdatedCode) => CreateGoogleAlice("Alice Updated"),
            (ExternalLoginConsts.GoogleProviderName, GoogleAliceCode) => CreateGoogleAlice("Alice"),
            (ExternalLoginConsts.GitHubProviderName, GitHubBobCode) => new ExternalIdentity
            {
                Provider = ExternalLoginConsts.GitHubProviderName,
                ProviderUserId = "github-bob-id",
                Email = "bob@example.test",
                DisplayName = "Bob",
                AvatarUrl = "https://example.test/bob.png"
            },
            _ => new ExternalIdentity
            {
                Provider = normalizedProvider,
                ProviderUserId = $"{normalizedProvider.ToLowerInvariant()}-{code}",
                Email = $"{code}@example.test",
                DisplayName = code,
                AvatarUrl = null
            }
        });
    }

    private static ExternalIdentity CreateGoogleAlice(string displayName)
    {
        return new ExternalIdentity
        {
            Provider = ExternalLoginConsts.GoogleProviderName,
            ProviderUserId = "google-alice-id",
            Email = "alice@example.test",
            DisplayName = displayName,
            AvatarUrl = "https://example.test/alice.png"
        };
    }
}
