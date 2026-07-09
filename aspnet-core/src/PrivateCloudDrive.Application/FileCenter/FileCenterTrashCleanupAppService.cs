using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 回收站清理建议应用服务。
/// 提供空间占用统计、保留天数和清理建议文案，帮助用户管理回收站。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public class FileCenterTrashCleanupAppService : FileCenterAppService, IFileCenterTrashCleanupAppService
{
    private const int DefaultRetentionDays = 30;

    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IClock _clock;

    /// <summary>
    /// 初始化 <see cref="FileCenterTrashCleanupAppService"/> 的新实例。
    /// </summary>
    public FileCenterTrashCleanupAppService(
        IFileNodeRepository fileNodeRepository,
        IAsyncQueryableExecuter asyncExecuter,
        IClock clock)
    {
        _fileNodeRepository = fileNodeRepository;
        _asyncExecuter = asyncExecuter;
        _clock = clock;
    }

    /// <summary>
    /// 获取当前用户回收站的清理建议。
    /// </summary>
    public virtual async Task<TrashCleanupAdviceDto> GetAdviceAsync()
    {
        var ownerId = GetOwnerId();

        // 获取回收站根节点
        var deletedRootsCount = await _fileNodeRepository.GetDeletedRootsCountAsync(ownerId, CurrentTenant.Id);
        var rootItems = await _fileNodeRepository.GetDeletedRootsAsync(
            ownerId,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId: CurrentTenant.Id);

        // 计算总空间占用和即将自动清理的项
        long totalSize = 0;
        int autoCleanupCount = 0;
        long autoCleanupSize = 0;
        int fileCount = 0;
        int folderCount = 0;
        var now = _clock.Now;

        foreach (var root in rootItems)
        {
            var subtreeNodes = await GetDeletedSubtreeNodesAsync(ownerId, root);
            var subtreeSize = subtreeNodes.Sum(n => n.Size);

            totalSize += subtreeSize;

            if (root.NodeType == FileNodeType.File)
                fileCount++;
            else
                folderCount++;

            // 检查是否超过保留天数
            if (root.DeletionTime.HasValue &&
                root.DeletionTime.Value.AddDays(DefaultRetentionDays) <= now)
            {
                autoCleanupCount++;
                autoCleanupSize += subtreeSize;
            }
        }

        return new TrashCleanupAdviceDto
        {
            TrashSizeBytes = totalSize,
            TrashFileCount = fileCount,
            TrashFolderCount = folderCount,
            RetentionDays = DefaultRetentionDays,
            AutoCleanupCount = autoCleanupCount,
            AutoCleanupSizeBytes = autoCleanupSize,
            CleanupAdviceMessage = BuildCleanupAdviceMessage(
                totalSize, (int)deletedRootsCount, autoCleanupCount, autoCleanupSize, DefaultRetentionDays)
        };
    }

    private async Task<List<FileNode>> GetDeletedSubtreeNodesAsync(Guid ownerId, FileNode root)
    {
        var nodes = new List<FileNode> { root };

        var children = await _fileNodeRepository.GetChildrenAsync(
            ownerId,
            root.Id,
            skipCount: 0,
            maxResultCount: int.MaxValue,
            tenantId: CurrentTenant.Id,
            includeDeleted: true);

        foreach (var child in children)
        {
            nodes.AddRange(await GetDeletedSubtreeNodesAsync(ownerId, child));
        }

        return nodes;
    }

    private static string BuildCleanupAdviceMessage(
        long totalSizeBytes, int itemCount, int autoCleanupCount, long autoCleanupSizeBytes, int retentionDays)
    {
        var sizeDisplay = FormatSize(totalSizeBytes);

        if (itemCount == 0)
        {
            return "回收站是空的，无需清理。";
        }

        if (autoCleanupCount == 0)
        {
            return $"回收站正在占用 {sizeDisplay}（{itemCount} 项）。" +
                   $"回收站内的项目将在删除 {retentionDays} 天后自动清理。您可以在设置中调整回收站保留策略。";
        }

        var autoSizeDisplay = FormatSize(autoCleanupSizeBytes);
        return $"回收站正在占用 {sizeDisplay}（{itemCount} 项）。" +
               $"其中 {autoCleanupCount} 项（{autoSizeDisplay}）已超过 {retentionDays} 天保留期，将在后台自动清理。" +
               $"您也可以在设置中调整回收站保留策略。";
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }
}
