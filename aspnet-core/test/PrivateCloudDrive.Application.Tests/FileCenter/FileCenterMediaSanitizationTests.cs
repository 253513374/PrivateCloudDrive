using Shouldly;
using Xunit;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// SanitizeProcessError 脱敏逻辑的纯函数测试。
/// 验证 Windows 路径、Unix 路径、Token、密码、ConnectionString 在入库前被正确脱敏。
/// </summary>
public class FileCenterMediaSanitizationTests
{
    [Fact]
    public void SanitizeProcessError_Should_Redact_Windows_Path()
    {
        var raw = "FFmpeg failed: C:\\Users\\testuser\\Videos\\clip.mp4 is corrupt";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("C:\\Users\\testuser\\Videos\\clip.mp4");
        result.ShouldContain("[path]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Unix_Path()
    {
        var raw = "FFprobe could not open /var/lib/media/clip.mp4";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("/var/lib/media/clip.mp4");
        result.ShouldContain("[path]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Token()
    {
        var raw = "Authentication failed: token=abc123def456";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("abc123def456");
        result.ShouldContain("token=[redacted]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Password()
    {
        var raw = "Upload failed: password=secret123";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("secret123");
        result.ShouldContain("password=[redacted]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_ConnectionString()
    {
        var raw = "Database error: connection string=Server=localhost;Database=test;User=admin;Password=p@ss;"; // redacted by design
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("Server=localhost");
        result.ShouldContain("connection string=[redacted]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Secret()
    {
        var raw = "API call failed: secret=my-super-secret-key-12345"; // redacted by design
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("my-super-secret-key-12345");
        result.ShouldContain("secret=[redacted]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Multiple_Sensitive_Values()
    {
        var raw = "FFmpeg at C:\\tools\\ffmpeg.exe failed for /var/media/clip.mp4 with token=abc password=xyz";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("C:\\tools\\ffmpeg.exe");
        result.ShouldNotContain("/var/media/clip.mp4");
        result.ShouldNotContain("abc");
        result.ShouldNotContain("xyz");
        result.ShouldContain("[path]");
        result.ShouldContain("token=[redacted]");
        result.ShouldContain("password=[redacted]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Return_Null_For_Null_Input()
    {
        FileCenterMediaLibraryHelpers.SanitizeProcessError(null).ShouldBeNull();
    }

    [Fact]
    public void SanitizeProcessError_Should_Return_Null_For_Empty_Input()
    {
        FileCenterMediaLibraryHelpers.SanitizeProcessError("").ShouldBeNull();
    }

    [Fact]
    public void SanitizeProcessError_Should_Return_Null_For_Whitespace_Input()
    {
        FileCenterMediaLibraryHelpers.SanitizeProcessError("   ").ShouldBeNull();
    }

    [Fact]
    public void SanitizeProcessError_Should_Truncate_At_180_Characters()
    {
        var longMessage = new string('A', 300);
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(longMessage);

        result.ShouldNotBeNull();
        result.Length.ShouldBe(180 + 3); // 180 chars + "..."
        result.ShouldEndWith("...");
    }

    [Fact]
    public void SanitizeProcessError_Should_Not_Truncate_Under_180()
    {
        var shortMessage = new string('A', 100);
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(shortMessage);

        result.ShouldNotBeNull();
        result.Length.ShouldBe(100);
        result.ShouldNotEndWith("...");
    }

    [Fact]
    public void SanitizeProcessError_Should_Preserve_Safe_Error()
    {
        var safe = "Media file node has no blob name.";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(safe);

        result.ShouldBe(safe);
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Ffmpeg_Executable_Path()
    {
        var raw = "Failed to start media processor 'C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe'. Ensure FFmpeg/FFprobe is installed or configure FileCenter:MediaProcessing paths.";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe");
        result.ShouldContain("[path]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Redact_Ffmpeg_Stderr_Path()
    {
        var raw = "Media processor '/usr/bin/ffmpeg' failed with exit code 1: /tmp/ffmpeg-input-abc123.mp4: Invalid data found when processing input";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldNotBeNull();
        result.ShouldNotContain("/usr/bin/ffmpeg");
        result.ShouldNotContain("/tmp/ffmpeg-input-abc123.mp4");
        result.ShouldContain("[path]");
    }

    [Fact]
    public void SanitizeProcessError_Should_Handle_Already_Sanitized_Input()
    {
        var raw = "FFmpeg at [path] failed for [path] with token=[redacted]";
        var result = FileCenterMediaLibraryHelpers.SanitizeProcessError(raw);

        result.ShouldBe("FFmpeg at [path] failed for [path] with token=[redacted]");
    }
}
