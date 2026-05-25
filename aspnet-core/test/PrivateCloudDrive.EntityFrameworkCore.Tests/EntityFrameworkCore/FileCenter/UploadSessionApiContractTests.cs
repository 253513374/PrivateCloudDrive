using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using PrivateCloudDrive.FileCenter;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 上传会话 API 契约级 QA 测试：覆盖 API-UploadSession-01 到 06 的响应字段、错误码和开放字符串兜底。
/// </summary>
public class UploadSessionApiContractTests
{
    private static readonly IReadOnlySet<string> RequiredUploadSessionDtoFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "Id",
        "TenantId",
        "OwnerId",
        "ParentId",
        "FileName",
        "TotalSize",
        "ChunkSize",
        "TotalChunks",
        "ContentType",
        "Sha256",
        "Status",
        "ExpirationTime",
        "FileNodeId",
        "UploadedChunks",
        "UploadedChunkCount",
        "UploadedBytes",
        "ProgressPercent",
        "IsRetryable",
        "StatusReason",
        "FailureReason",
        "NextAction"
    };

    private static readonly IReadOnlySet<string> KnownStatusReasons = new HashSet<string>(StringComparer.Ordinal)
    {
        "WaitingForChunks",
        "Completed",
        "Cancelled",
        "Unknown"
    };

    private static readonly IReadOnlySet<string> KnownNextActions = new HashSet<string>(StringComparer.Ordinal)
    {
        "UploadMissingChunks",
        "OpenFile",
        "StartNewUploadSession"
    };

    public static IEnumerable<object[]> FixtureFiles => new[]
    {
        new object[] { "API-UploadSession-01", "created-after-get.json" },
        new object[] { "API-UploadSession-02", "chunk-uploaded.json" },
        new object[] { "API-UploadSession-03", "duplicate-chunk-dedup.json" },
        new object[] { "API-UploadSession-04", "completed.json" },
        new object[] { "API-UploadSession-05", "cancelled-operation-error.json" },
        new object[] { "API-UploadSession-06", "unknown-status-fallback.json" }
    };

    [Fact]
    public void UploadSessionDto_Should_Expose_All_Documented_Response_Fields()
    {
        var actualFields = typeof(UploadSessionDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingFields = RequiredUploadSessionDtoFields
            .Where(field => !actualFields.Contains(field))
            .ToArray();

        missingFields.ShouldBeEmpty();
    }

    [Fact]
    public void DomainErrorCodes_Should_Reserve_Cancelled_Upload_Session_Error_Code_000033()
    {
        var errorCodes = typeof(PrivateCloudDriveDomainErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        errorCodes.ShouldContain("PrivateCloudDrive:FileCenter:000033");
    }

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void Mock_Fixtures_Should_Contain_Field_Level_Assertions_For_UploadSession_Apis(
        string caseId,
        string fixtureFile)
    {
        using var document = LoadFixture(fixtureFile);
        var root = document.RootElement;

        root.GetProperty("caseId").GetString().ShouldBe(caseId);
        root.TryGetProperty("assertions", out var assertions).ShouldBeTrue();
        assertions.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);

        foreach (var assertion in assertions.EnumerateArray())
        {
            assertion.TryGetProperty("field", out var field).ShouldBeTrue();
            field.GetString().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Mock_Fixture_Should_Keep_Unknown_StatusReason_And_NextAction_As_Open_Strings()
    {
        using var document = LoadFixture("unknown-status-fallback.json");
        var response = document.RootElement.GetProperty("response");
        var statusReason = response.GetProperty("statusReason").GetString();
        var nextAction = response.GetProperty("nextAction").GetString();

        statusReason.ShouldNotBeNullOrWhiteSpace();
        nextAction.ShouldNotBeNullOrWhiteSpace();
        KnownStatusReasons.ShouldNotContain(statusReason);
        KnownNextActions.ShouldNotContain(nextAction);
    }

    [Fact]
    public void Cancelled_Operation_Fixture_Should_Assert_Documented_Error_Code_000033()
    {
        using var document = LoadFixture("cancelled-operation-error.json");
        var error = document.RootElement.GetProperty("error");

        error.GetProperty("code").GetString().ShouldBe("PrivateCloudDrive:FileCenter:000033");
        error.GetProperty("httpStatus").GetInt32().ShouldBe(409);
    }

    private static JsonDocument LoadFixture(string fixtureFile)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "MockFixtures",
            "FileCenter",
            "UploadSessions",
            fixtureFile);

        File.Exists(path).ShouldBeTrue($"Mock fixture not found: {path}");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
