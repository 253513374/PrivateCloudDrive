using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Volo.Abp;

namespace PrivateCloudDrive.QaTesting;

/// <summary>
/// 从 PCD_QA_TEST_ACCOUNT_* 环境变量解析 QA 测试账号 seed 契约。
/// </summary>
public sealed class QaTestAccountSeedOptions
{
    public bool Enabled { get; private init; }
    public bool ForceRotate { get; private init; }
    public string? Password { get; private init; }
    public string? PasswordSource { get; private init; }

    public static QaTestAccountSeedOptions Load(IConfiguration? configuration = null)
    {
        var enabled = IsTrue(Read(configuration, QaTestAccountConsts.EnabledEnv));
        var forceRotate = IsTrue(Read(configuration, QaTestAccountConsts.ForceRotateEnv));

        if (!enabled)
        {
            return new QaTestAccountSeedOptions
            {
                Enabled = false,
                ForceRotate = forceRotate
            };
        }

        var inlinePassword = Read(configuration, QaTestAccountConsts.PasswordEnv);
        if (!inlinePassword.IsNullOrWhiteSpace())
        {
            return Create(enabled, forceRotate, inlinePassword!, QaTestAccountConsts.PasswordEnv);
        }

        var passwordFile = Read(configuration, QaTestAccountConsts.PasswordFileEnv);
        if (!passwordFile.IsNullOrWhiteSpace())
        {
            if (!File.Exists(passwordFile))
            {
                throw SecretContractException(
                    "PCD:QaTestAccount:MissingPasswordFile",
                    QaTestAccountConsts.PasswordFileEnv,
                    $"{QaTestAccountConsts.PasswordFileEnv} points to a file that does not exist.");
            }

            return Create(
                enabled,
                forceRotate,
                File.ReadAllText(passwordFile!).TrimEnd('\r', '\n'),
                QaTestAccountConsts.PasswordFileEnv);
        }

        throw SecretContractException(
            "PCD:QaTestAccount:MissingPassword",
            QaTestAccountConsts.PasswordEnv,
            $"{QaTestAccountConsts.PasswordEnv} or {QaTestAccountConsts.PasswordFileEnv} is required when {QaTestAccountConsts.EnabledEnv}=true.");
    }

    public static QaTestAccountSeedOptions FromEnvironment()
    {
        return Load();
    }

    private static QaTestAccountSeedOptions Create(bool enabled, bool forceRotate, string password, string source)
    {
        if (password.Length < QaTestAccountConsts.MinimumPasswordLength)
        {
            throw SecretContractException(
                "PCD:QaTestAccount:WeakPassword",
                source,
                $"QA test account password from {source} must be at least {QaTestAccountConsts.MinimumPasswordLength} characters.");
        }

        return new QaTestAccountSeedOptions
        {
            Enabled = enabled,
            ForceRotate = forceRotate,
            Password = password,
            PasswordSource = source
        };
    }

    private static string? Read(IConfiguration? configuration, string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? configuration?[name];
    }

    private static bool IsTrue(string? value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static BusinessException SecretContractException(string code, string secretName, string message)
    {
        var exception = new BusinessException(code, message: message);
        exception.Data["SecretName"] = secretName;
        return exception;
    }
}
