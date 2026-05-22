using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace PrivateCloudDrive.QaTesting;

public class QaTestAccountSeedOptionsTests
{
    [Fact]
    public void Load_Should_NoOp_When_Disabled()
    {
        using var _ = new EnvScope();
        var options = QaTestAccountSeedOptions.Load(new ConfigurationBuilder().Build());
        options.Enabled.ShouldBeFalse();
        options.Password.ShouldBeNull();
    }

    [Fact]
    public void Load_Should_Fail_When_Enabled_But_Secret_Missing()
    {
        using var _ = new EnvScope((QaTestAccountConsts.EnabledEnv, "true"));
        var exception = Should.Throw<BusinessException>(() => QaTestAccountSeedOptions.Load(new ConfigurationBuilder().Build()));
        exception.Code.ShouldBe("PCD:QaTestAccount:MissingPassword");
        exception.Data["SecretName"].ShouldBe(QaTestAccountConsts.PasswordEnv);
    }

    [Fact]
    public void Load_Should_Fail_Without_Printing_Weak_Secret()
    {
        const string weakSecret = "Tiny7!";
        using var _ = new EnvScope((QaTestAccountConsts.EnabledEnv, "true"), (QaTestAccountConsts.PasswordEnv, weakSecret));
        var exception = Should.Throw<BusinessException>(() => QaTestAccountSeedOptions.Load(new ConfigurationBuilder().Build()));
        exception.Code.ShouldBe("PCD:QaTestAccount:WeakPassword");
        exception.ToString().ShouldNotContain(weakSecret);
        exception.Data["SecretName"].ShouldBe(QaTestAccountConsts.PasswordEnv);
    }

    [Fact]
    public void Load_Should_Read_Secret_File_And_Force_Rotate()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "Qa-Test-Secret-123!\n");
        try
        {
            using var _ = new EnvScope((QaTestAccountConsts.EnabledEnv, "true"), (QaTestAccountConsts.ForceRotateEnv, "true"), (QaTestAccountConsts.PasswordFileEnv, path));
            var options = QaTestAccountSeedOptions.Load(new ConfigurationBuilder().Build());
            options.Enabled.ShouldBeTrue();
            options.ForceRotate.ShouldBeTrue();
            options.Password.ShouldBe("Qa-Test-Secret-123!");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void GrantedPermissions_Should_Exclude_Admin_And_Audit_Permissions()
    {
        QaTestAccountConsts.GrantedPermissions.ShouldContain("PrivateCloudDrive.FileCenter.Upload");
        QaTestAccountConsts.GrantedPermissions.ShouldNotContain(QaTestAccountConsts.ForbiddenPermissions[0]);
        QaTestAccountConsts.GrantedPermissions.ShouldNotContain(QaTestAccountConsts.ForbiddenPermissions[1]);
        QaTestAccountConsts.GrantedPermissions.ShouldNotContain(QaTestAccountConsts.ForbiddenPermissions[2]);
    }

    [Fact]
    public void Environment_Contract_Should_Use_Pcd_Qa_Test_Account_Prefix()
    {
        QaTestAccountConsts.EnabledEnv.ShouldStartWith("PCD_QA_TEST_ACCOUNT_");
        QaTestAccountConsts.PasswordEnv.ShouldStartWith("PCD_QA_TEST_ACCOUNT_");
        QaTestAccountConsts.PasswordFileEnv.ShouldStartWith("PCD_QA_TEST_ACCOUNT_");
        QaTestAccountConsts.ForceRotateEnv.ShouldStartWith("PCD_QA_TEST_ACCOUNT_");
        QaTestAccountConsts.SkipMigratorEnv.ShouldStartWith("PCD_QA_TEST_ACCOUNT_");
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original = new();
        public EnvScope(params (string Key, string? Value)[] values)
        {
            var keys = new[] { QaTestAccountConsts.EnabledEnv, QaTestAccountConsts.PasswordEnv, QaTestAccountConsts.PasswordFileEnv, QaTestAccountConsts.ForceRotateEnv, QaTestAccountConsts.SkipMigratorEnv };
            foreach (var key in keys) { _original[key] = Environment.GetEnvironmentVariable(key); Environment.SetEnvironmentVariable(key, null); }
            foreach (var (key, value) in values) { Environment.SetEnvironmentVariable(key, value); }
        }
        public void Dispose()
        {
            foreach (var (key, value) in _original) { Environment.SetEnvironmentVariable(key, value); }
        }
    }
}
