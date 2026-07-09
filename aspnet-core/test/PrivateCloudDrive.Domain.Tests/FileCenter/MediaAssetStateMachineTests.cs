using System;
using Shouldly;
using Xunit;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 验证 MediaAsset 状态机转换规则的域层测试。
/// 状态机约束：Pending→Processing→Completed/Failed
/// </summary>
public class MediaAssetStateMachineTests
{
    private static MediaAsset CreatePendingAsset()
    {
        return MediaAsset.CreatePending(
            Guid.NewGuid(),
            tenantId: null,
            ownerId: Guid.NewGuid(),
            fileNodeId: Guid.NewGuid(),
            MediaAssetMediaType.Image);
    }

    [Fact]
    public void Should_Start_As_Pending()
    {
        var asset = CreatePendingAsset();
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Pending);
    }

    [Fact]
    public void Should_Allow_MarkProcessing_From_Pending()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Processing);
        asset.ProcessError.ShouldBeNull();
    }

    [Fact]
    public void Should_Allow_MarkProcessing_From_Failed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkFailed("test error");
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Failed);

        // Retry: back to Processing
        asset.MarkProcessing();
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Processing);
        asset.ProcessError.ShouldBeNull();
    }

    [Fact]
    public void Should_Throw_When_MarkProcessing_From_Processing()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();

        Should.Throw<InvalidOperationException>(() => asset.MarkProcessing());
    }

    [Fact]
    public void Should_Throw_When_MarkProcessing_From_Completed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkImageProcessed(100, 200, null, Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => asset.MarkProcessing());
    }

    [Fact]
    public void Should_Allow_MarkFailed_From_Processing()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();

        asset.MarkFailed("processing error");
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Failed);
        asset.ProcessError.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Throw_When_MarkFailed_From_Pending()
    {
        var asset = CreatePendingAsset();

        Should.Throw<InvalidOperationException>(() => asset.MarkFailed("error"));
    }

    [Fact]
    public void Should_Throw_When_MarkFailed_From_Completed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkImageProcessed(100, 200, null, Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => asset.MarkFailed("error"));
    }

    [Fact]
    public void Should_Throw_When_MarkFailed_From_Failed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkFailed("first error");

        Should.Throw<InvalidOperationException>(() => asset.MarkFailed("second error"));
    }

    [Fact]
    public void Should_Allow_MarkImageProcessed_From_Processing()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();

        asset.MarkImageProcessed(100, 200, null, Guid.NewGuid());
        asset.ProcessStatus.ShouldBe(MediaAssetProcessStatus.Completed);
    }

    [Fact]
    public void Should_Throw_When_MarkImageProcessed_From_Pending()
    {
        var asset = CreatePendingAsset();

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkImageProcessed(100, 200, null, Guid.NewGuid()));
    }

    [Fact]
    public void Should_Throw_When_MarkImageProcessed_From_Failed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkFailed("error");

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkImageProcessed(100, 200, null, Guid.NewGuid()));
    }

    [Fact]
    public void Should_Throw_When_MarkImageProcessed_From_Completed()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();
        asset.MarkImageProcessed(100, 200, null, Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkImageProcessed(200, 300, null, Guid.NewGuid()));
    }

    [Fact]
    public void Should_Throw_When_MarkVideoProcessed_From_Pending()
    {
        var asset = CreatePendingAsset();

        Should.Throw<InvalidOperationException>(() =>
            asset.MarkVideoProcessed(640, 480, 12345, "h264", Guid.NewGuid()));
    }

    [Fact]
    public void Should_Truncate_Error_To_180_Chars()
    {
        var asset = CreatePendingAsset();
        asset.MarkProcessing();

        var longError = new string('A', 500);
        asset.MarkFailed(longError);
        asset.ProcessError.ShouldNotBeNull();
        asset.ProcessError.Length.ShouldBeLessThanOrEqualTo(180);
    }
}
