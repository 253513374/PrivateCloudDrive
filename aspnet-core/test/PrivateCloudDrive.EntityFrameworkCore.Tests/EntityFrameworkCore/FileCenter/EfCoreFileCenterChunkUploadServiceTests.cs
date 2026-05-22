using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心EfCoreFileCenterChunkUploadServiceTests，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
[Collection(PrivateCloudDriveTestConsts.CollectionDefinitionName)]
public class EfCoreFileCenterChunkUploadServiceTests : PrivateCloudDriveEntityFrameworkCoreTestBase
{
    private readonly PrivateCloudDrive.FileCenter.IFileCenterChunkUploadService _chunkUploadService;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService _fileDownloadService;
    private readonly IRepository<PrivateCloudDrive.FileCenter.UploadSession, Guid> _uploadSessionRepository;
    private readonly PrivateCloudDrive.FileCenter.IFileCenterBlobStoragePathProvider _storagePathProvider;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    /// <summary>
    /// 初始化 <see cref="EfCoreFileCenterChunkUploadServiceTests"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public EfCoreFileCenterChunkUploadServiceTests()
    {
        _chunkUploadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterChunkUploadService>();
        _fileDownloadService = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterFileDownloadService>();
        _uploadSessionRepository = GetRequiredService<IRepository<PrivateCloudDrive.FileCenter.UploadSession, Guid>>();
        _storagePathProvider = GetRequiredService<PrivateCloudDrive.FileCenter.IFileCenterBlobStoragePathProvider>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Complete_Chunk_Upload_And_Create_File()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("chunk upload creates one complete file");
        var chunkSize = 7;
        var chunks = Split(content, chunkSize);
        var sha256 = ComputeSha256(content);

        await WithCurrentUserAsync(userId, async () =>
        {
            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "large-video.mp4",
                    ContentType = "video/mp4",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count,
                    Sha256 = sha256
                });

            session.UploadedChunkCount.ShouldBe(0);
            session.UploadedBytes.ShouldBe(0);
            session.ProgressPercent.ShouldBe(0m);
            session.IsRetryable.ShouldBeTrue();
            session.StatusReason.ShouldBe("WaitingForChunks");
            session.FailureReason.ShouldBeNull();
            session.NextAction.ShouldBe("UploadMissingChunks");

            await UploadChunkAsync(session.Id, 0, chunks[0]);

            var inProgressSession = await _chunkUploadService.GetAsync(session.Id);
            inProgressSession.UploadedChunks.ShouldBe(new[] { 0 });
            inProgressSession.UploadedChunkCount.ShouldBe(1);
            inProgressSession.UploadedBytes.ShouldBe(chunks[0].Length);
            inProgressSession.ProgressPercent.ShouldBe(Math.Round(chunks[0].Length * 100m / content.Length, 2, MidpointRounding.AwayFromZero));
            inProgressSession.IsRetryable.ShouldBeTrue();
            inProgressSession.StatusReason.ShouldBe("WaitingForChunks");
            inProgressSession.FailureReason.ShouldBeNull();
            inProgressSession.NextAction.ShouldBe("UploadMissingChunks");

            for (var chunkIndex = 1; chunkIndex < chunks.Count; chunkIndex++)
            {
                await UploadChunkAsync(session.Id, chunkIndex, chunks[chunkIndex]);
            }

            var fileNode = await _chunkUploadService.CompleteAsync(session.Id);

            fileNode.Name.ShouldBe("large-video.mp4");
            fileNode.Size.ShouldBe(content.Length);
            fileNode.ContentType.ShouldBe("video/mp4");
            fileNode.NodeType.ShouldBe(PrivateCloudDrive.FileCenter.FileNodeType.File);

            var completedSession = await WithUnitOfWorkAsync(async () =>
                await _uploadSessionRepository.GetAsync(session.Id));

            completedSession.Status.ShouldBe(PrivateCloudDrive.FileCenter.UploadSessionStatus.Completed);
            completedSession.FileNodeId.ShouldBe(fileNode.Id);

            var completedSessionDto = await _chunkUploadService.GetAsync(session.Id);
            completedSessionDto.UploadedChunkCount.ShouldBe(chunks.Count);
            completedSessionDto.UploadedBytes.ShouldBe(content.Length);
            completedSessionDto.ProgressPercent.ShouldBe(100m);
            completedSessionDto.IsRetryable.ShouldBeFalse();
            completedSessionDto.StatusReason.ShouldBe("Completed");
            completedSessionDto.FailureReason.ShouldBeNull();
            completedSessionDto.NextAction.ShouldBe("OpenFile");

            var download = await _fileDownloadService.GetDownloadAsync(fileNode.Id);
            await using (download.Content)
            {
                using var downloaded = new MemoryStream();
                await download.Content.CopyToAsync(downloaded);

                downloaded.ToArray().ShouldBe(content);
            }
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Complete_When_Sha256_Does_Not_Match()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("hash mismatch");
        var chunkSize = 4;
        var chunks = Split(content, chunkSize);

        await WithCurrentUserAsync(userId, async () =>
        {
            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "bad-hash.bin",
                    ContentType = "application/octet-stream",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count,
                    Sha256 = new string('0', 64)
                });

            for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                await UploadChunkAsync(session.Id, chunkIndex, chunks[chunkIndex]);
            }

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _chunkUploadService.CompleteAsync(session.Id);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionHashMismatch);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Chunk_When_Size_Does_Not_Match_Expected_Size()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("chunk size mismatch");
        var chunkSize = 5;
        var chunks = Split(content, chunkSize);

        await WithCurrentUserAsync(userId, async () =>
        {
            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "size-mismatch.bin",
                    ContentType = "application/octet-stream",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count
                });

            await using var wrongSizedStream = new MemoryStream(chunks[0].Take(chunks[0].Length - 1).ToArray());

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _chunkUploadService.UploadChunkAsync(
                    session.Id,
                    0,
                    wrongSizedStream,
                    wrongSizedStream.Length);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterUploadChunkSizeMismatch);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Reject_Complete_When_Not_All_Chunks_Are_Uploaded()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("incomplete upload session");
        var chunkSize = 6;
        var chunks = Split(content, chunkSize);

        await WithCurrentUserAsync(userId, async () =>
        {
            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "incomplete.bin",
                    ContentType = "application/octet-stream",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count
                });

            await UploadChunkAsync(session.Id, 0, chunks[0]);

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _chunkUploadService.CompleteAsync(session.Id);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionIncomplete);
        });
    }

    /// <summary>
    /// 验证对应业务场景的预期行为，防止后续变更破坏既有规则。
    /// </summary>
    [Fact]
    public async Task Should_Cancel_Session_And_Delete_Temporary_Chunks()
    {
        var userId = Guid.NewGuid();
        var content = Encoding.UTF8.GetBytes("cancel upload");
        var chunkSize = 6;
        var chunks = Split(content, chunkSize);

        await WithCurrentUserAsync(userId, async () =>
        {
            var session = await _chunkUploadService.CreateAsync(
                new PrivateCloudDrive.FileCenter.CreateUploadSessionInput
                {
                    FileName = "cancel.txt",
                    ContentType = "text/plain",
                    TotalSize = content.Length,
                    ChunkSize = chunkSize,
                    TotalChunks = chunks.Count
                });

            await UploadChunkAsync(session.Id, 0, chunks[0]);

            var sessionDirectory = GetSessionDirectory(userId, session.Id);
            Directory.Exists(sessionDirectory).ShouldBeTrue();

            await _chunkUploadService.CancelAsync(session.Id);

            Directory.Exists(sessionDirectory).ShouldBeFalse();

            var cancelledSession = await WithUnitOfWorkAsync(async () =>
                await _uploadSessionRepository.GetAsync(session.Id));

            cancelledSession.Status.ShouldBe(PrivateCloudDrive.FileCenter.UploadSessionStatus.Cancelled);

            var cancelledSessionDto = await _chunkUploadService.GetAsync(session.Id);
            cancelledSessionDto.UploadedChunkCount.ShouldBe(1);
            cancelledSessionDto.UploadedBytes.ShouldBe(chunks[0].Length);
            cancelledSessionDto.ProgressPercent.ShouldBe(Math.Round(chunks[0].Length * 100m / content.Length, 2, MidpointRounding.AwayFromZero));
            cancelledSessionDto.IsRetryable.ShouldBeFalse();
            cancelledSessionDto.StatusReason.ShouldBe("Cancelled");
            cancelledSessionDto.FailureReason.ShouldBe("Cancelled");
            cancelledSessionDto.NextAction.ShouldBe("StartNewUploadSession");

            var exception = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await UploadChunkAsync(session.Id, 1, chunks[1]);
            });

            exception.Code.ShouldBe(PrivateCloudDriveDomainErrorCodes.FileCenterUploadSessionCancelled);
        });
    }

    private async Task UploadChunkAsync(Guid sessionId, int chunkIndex, byte[] content)
    {
        await using var stream = new MemoryStream(content);

        await _chunkUploadService.UploadChunkAsync(
            sessionId,
            chunkIndex,
            stream,
            content.Length);
    }

    private string GetSessionDirectory(Guid userId, Guid sessionId)
    {
        return Path.GetFullPath(
            Path.Combine(
                _storagePathProvider.GetStorageRootPath(),
                "temp",
                "uploads",
                "host",
                userId.ToString("N"),
                sessionId.ToString("N")));
    }

    private async Task WithCurrentUserAsync(Guid userId, Func<Task> action)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(AbpClaimTypes.UserId, userId.ToString()),
                    new Claim(AbpClaimTypes.UserName, "chunk-upload-test")
                },
                "Test"));

        using (_currentPrincipalAccessor.Change(principal))
        {
            await action();
        }
    }

    private static List<byte[]> Split(byte[] content, int chunkSize)
    {
        var chunks = new List<byte[]>();
        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            chunks.Add(content.Skip(offset).Take(chunkSize).ToArray());
        }

        return chunks;
    }

    private static string ComputeSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }
}
