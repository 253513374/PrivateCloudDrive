using System;
using System.Security.Cryptography;
using System.Text;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 分享密码哈希算法测试，覆盖新 KDF 格式与旧 SHA-256 数据兼容。
/// </summary>
public class FileCenterSharePasswordHashTests
{
    [Fact]
    public void CreatePasswordHash_Should_Use_Versioned_Pbkdf2_Format()
    {
        var (salt, hash) = FileCenterSharesAppService.CreatePasswordHash("secret-password");

        salt.ShouldNotBeNull();
        hash.ShouldNotBeNull();
        salt!.ShouldStartWith("pbkdf2-v1:");
        salt.Length.ShouldBeLessThanOrEqualTo(FileShareConsts.MaxPasswordSaltLength);
        hash!.Length.ShouldBe(64);
        hash.ShouldNotBe(ComputeLegacyHash(salt, "secret-password"));
    }

    [Fact]
    public void VerifyPassword_Should_Accept_Correct_Pbkdf2_Password_And_Reject_Wrong_Password()
    {
        var (salt, hash) = FileCenterSharesAppService.CreatePasswordHash("secret-password");
        var share = CreateShare(salt, hash);

        FileCenterSharesAppService.VerifyPassword(share, "secret-password").ShouldBeTrue();
        FileCenterSharesAppService.VerifyPassword(share, "wrong-password").ShouldBeFalse();
    }

    [Fact]
    public void VerifyPassword_Should_Remain_Compatible_With_Legacy_Sha256_Hash()
    {
        const string legacySalt = "00112233445566778899aabbccddeeff";
        var legacyHash = ComputeLegacyHash(legacySalt, "legacy-password");
        var share = CreateShare(legacySalt, legacyHash);

        FileCenterSharesAppService.VerifyPassword(share, "legacy-password").ShouldBeTrue();
        FileCenterSharesAppService.VerifyPassword(share, "wrong-password").ShouldBeFalse();
    }

    private static FileShare CreateShare(string? salt, string? hash)
    {
        return new FileShare(
            Guid.NewGuid(),
            tenantId: null,
            ownerId: Guid.NewGuid(),
            fileNodeId: Guid.NewGuid(),
            token: FileCenterSharesAppService.CreateToken(),
            expirationTime: DateTime.Now.AddDays(1),
            allowDownload: true,
            passwordSalt: salt,
            passwordHash: hash);
    }

    private static string ComputeLegacyHash(string salt, string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
