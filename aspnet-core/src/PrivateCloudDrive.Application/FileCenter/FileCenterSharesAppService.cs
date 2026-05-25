using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PrivateCloudDrive.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 登录用户的文件分享管理应用服务。
/// 负责创建分享、列出个人分享、管理员查看全量分享以及禁用分享链接。
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Share)]
public class FileCenterSharesAppService : FileCenterAppService, IFileCenterSharesAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IRepository<FileShare, Guid> _shareRepository;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    /// <summary>
    /// 初始化 <see cref="FileCenterSharesAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterSharesAppService(
        IGuidGenerator guidGenerator,
        IClock clock,
        IRepository<FileShare, Guid> shareRepository,
        IFileNodeRepository fileNodeRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _guidGenerator = guidGenerator;
        _clock = clock;
        _shareRepository = shareRepository;
        _fileNodeRepository = fileNodeRepository;
        _asyncExecuter = asyncExecuter;
    }

    /// <summary>
    /// 为当前用户拥有的文件或文件夹创建分享链接；可选密码会以盐和哈希保存。
    /// </summary>
    public virtual async Task<FileShareDto> CreateAsync(CreateFileShareInput input)
    {
        var ownerId = GetOwnerId();
        var node = await GetOwnerNodeAsync(ownerId, input.FileNodeId);
        var (passwordSalt, passwordHash) = CreatePasswordHash(input.Password);
        var token = await CreateUniqueTokenAsync();

        var share = new FileShare(
            _guidGenerator.Create(),
            CurrentTenant.Id,
            ownerId,
            node.Id,
            token,
            input.ExpirationTime,
            input.AllowDownload,
            passwordSalt,
            passwordHash);

        await _shareRepository.InsertAsync(share, autoSave: true);

        return ToDto(share, node);
    }

    /// <summary>
    /// 获取当前用户的分享列表，包含已禁用和已过期分享，便于客户端管理。
    /// </summary>
    public virtual async Task<PagedResultDto<FileShareDto>> GetListAsync(PagedResultRequestDto input)
    {
        var ownerId = GetOwnerId();
        var queryable = (await _shareRepository.GetQueryableAsync())
            .Where(share =>
                share.TenantId == CurrentTenant.Id &&
                share.OwnerId == ownerId)
            .OrderByDescending(share => share.CreationTime);

        var totalCount = await _asyncExecuter.LongCountAsync(queryable);
        var shares = await _asyncExecuter.ToListAsync(
            queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        var items = new List<FileShareDto>();
        foreach (var share in shares)
        {
            var node = await _fileNodeRepository.FindByIdAsync(share.FileNodeId, ownerId, CurrentTenant.Id);
            if (node != null)
            {
                items.Add(ToDto(share, node));
            }
        }

        return new PagedResultDto<FileShareDto>(totalCount, items);
    }

    /// <summary>
    /// 管理员查看当前租户下全部分享，包含已删除文件节点关联的历史分享。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task<PagedResultDto<FileShareDto>> GetAllListAsync(PagedResultRequestDto input)
    {
        var queryable = (await _shareRepository.GetQueryableAsync())
            .Where(share => share.TenantId == CurrentTenant.Id)
            .OrderByDescending(share => share.CreationTime);

        var totalCount = await _asyncExecuter.LongCountAsync(queryable);
        var shares = await _asyncExecuter.ToListAsync(
            queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        var items = new List<FileShareDto>();
        foreach (var share in shares)
        {
            var node = await _fileNodeRepository.FindByIdAsync(
                share.FileNodeId,
                share.OwnerId,
                CurrentTenant.Id,
                includeDeleted: true);

            items.Add(ToDto(share, node));
        }

        return new PagedResultDto<FileShareDto>(totalCount, items);
    }

    /// <summary>
    /// 当前用户禁用自己的分享链接。
    /// </summary>
    public virtual async Task DeleteAsync(Guid id)
    {
        var ownerId = GetOwnerId();
        var share = await _shareRepository.FirstOrDefaultAsync(
            item =>
                item.Id == id &&
                item.TenantId == CurrentTenant.Id &&
                item.OwnerId == ownerId &&
                item.IsEnabled);

        if (share == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareNotFound)
                .WithData("Id", id);
        }

        share.Disable();
        await _shareRepository.UpdateAsync(share, autoSave: true);
    }

    /// <summary>
    /// 管理员禁用任意分享链接，用于违规内容或安全风险处置。
    /// </summary>
    [Authorize(PrivateCloudDrivePermissions.FileCenter.Manage)]
    public virtual async Task DisableAsync(Guid id)
    {
        var share = await _shareRepository.FirstOrDefaultAsync(
            item =>
                item.Id == id &&
                item.TenantId == CurrentTenant.Id &&
                item.IsEnabled);

        if (share == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareNotFound)
                .WithData("Id", id);
        }

        share.Disable();
        await _shareRepository.UpdateAsync(share, autoSave: true);
    }

    private async Task<FileNode> GetOwnerNodeAsync(Guid ownerId, Guid nodeId)
    {
        var node = await _fileNodeRepository.FindByIdAsync(nodeId, ownerId, CurrentTenant.Id);
        if (node == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", nodeId);
        }

        return node;
    }

    private async Task<string> CreateUniqueTokenAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = CreateToken();
            var existing = await _shareRepository.FirstOrDefaultAsync(share => share.Token == token);
            if (existing == null)
            {
                return token;
            }
        }

        throw new InvalidOperationException("Unable to create a unique share token.");
    }

    private Guid GetOwnerId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Current user is required for FileCenter operations.");
        }

        return CurrentUser.Id.Value;
    }

    private FileShareDto ToDto(FileShare share, FileNode? node)
    {
        return new FileShareDto
        {
            Id = share.Id,
            TenantId = share.TenantId,
            OwnerId = share.OwnerId,
            FileNodeId = share.FileNodeId,
            FileName = node?.Name ?? "Deleted item",
            NodeType = node?.NodeType ?? FileNodeType.File,
            Token = share.Token,
            ExpirationTime = share.ExpirationTime,
            CreationTime = share.CreationTime,
            AllowDownload = share.AllowDownload,
            RequiresPassword = share.RequiresPassword,
            VisitCount = share.VisitCount,
            IsEnabled = share.IsEnabled,
            IsExpired = share.ExpirationTime.HasValue && share.ExpirationTime.Value <= _clock.Now
        };
    }

    internal static string CreateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    internal static (string? Salt, string? Hash) CreatePasswordHash(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (null, null);
        }

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        return (salt, ComputePasswordHash(salt, password));
    }

    internal static bool VerifyPassword(FileShare share, string? password)
    {
        if (!share.RequiresPassword)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(share.PasswordSalt) ||
            string.IsNullOrWhiteSpace(share.PasswordHash))
        {
            return false;
        }

        var computedHash = ComputePasswordHash(share.PasswordSalt, password);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(share.PasswordHash));
    }

    private static string ComputePasswordHash(string salt, string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// 公开分享访问应用服务。
/// 允许匿名用户通过 token 读取分享元数据、校验密码和下载被分享文件。
/// 仅供显式 HTTP 控制器调用，禁止 ABP 将该应用服务暴露为约定式控制器。
/// </summary>
[AllowAnonymous]
[RemoteService(false)]
public class FileCenterPublicSharesAppService : FileCenterAppService, IFileCenterPublicSharesAppService
{
    private readonly IClock _clock;
    private readonly IRepository<FileShare, Guid> _shareRepository;
    private readonly IFileNodeRepository _fileNodeRepository;
    private readonly IFileCenterBlobContentReader _blobContentReader;

    /// <summary>
    /// 初始化 <see cref="FileCenterPublicSharesAppService"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public FileCenterPublicSharesAppService(
        IClock clock,
        IRepository<FileShare, Guid> shareRepository,
        IFileNodeRepository fileNodeRepository,
        IFileCenterBlobContentReader blobContentReader)
    {
        _clock = clock;
        _shareRepository = shareRepository;
        _fileNodeRepository = fileNodeRepository;
        _blobContentReader = blobContentReader;
    }

    /// <summary>
    /// 根据分享 token 获取公开分享信息；如分享设置密码，则不会返回可下载内容。
    /// </summary>
    public virtual async Task<PublicFileShareDto> GetAsync(string token)
    {
        var (share, node) = await GetShareAndNodeAsync(token);
        if (share.RequiresPassword)
        {
            return ToPublicDto(share, node, passwordRequired: true);
        }

        share.IncreaseVisitCount();
        await _shareRepository.UpdateAsync(share, autoSave: true);

        return ToPublicDto(share, node, passwordRequired: false);
    }

    /// <summary>
    /// 校验分享密码，成功后返回可访问的分享信息。明文密码只用于本次哈希比对。
    /// </summary>
    public virtual async Task<PublicFileShareDto> VerifyPasswordAsync(string token, VerifySharePasswordInput input)
    {
        var (share, node) = await GetShareAndNodeAsync(token);
        EnsurePassword(share, input.Password);

        share.IncreaseVisitCount();
        await _shareRepository.UpdateAsync(share, autoSave: true);

        return ToPublicDto(share, node, passwordRequired: false);
    }

    /// <summary>
    /// 获取公开分享文件的下载流；仅文件分享且允许下载时可用。
    /// </summary>
    public virtual async Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        return await GetDownloadAsync(token, password, range: null, cancellationToken);
    }

    public virtual async Task<FileDownloadInfo> GetDownloadAsync(
        string token,
        string? password,
        FileDownloadRangeRequest? range,
        CancellationToken cancellationToken = default)
    {
        var (share, node) = await GetShareAndNodeAsync(token);
        EnsurePassword(share, password);

        if (!share.AllowDownload)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareDownloadDisabled);
        }

        if (node.NodeType != FileNodeType.File || string.IsNullOrWhiteSpace(node.BlobName))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterOnlyFileCanBeDownloaded)
                .WithData("Id", node.Id);
        }

        var normalizedRange = range?.Normalize(node.Size);
        var stream = normalizedRange == null
            ? await _blobContentReader.OpenReadAsync(node.BlobName, cancellationToken)
            : await _blobContentReader.OpenReadRangeAsync(
                node.BlobName,
                normalizedRange.Start,
                normalizedRange.End,
                cancellationToken);

        share.IncreaseVisitCount();
        await _shareRepository.UpdateAsync(share, autoSave: true, cancellationToken);

        return new FileDownloadInfo
        {
            Content = stream,
            FileName = node.Name,
            ContentType = node.ContentType ?? "application/octet-stream",
            Size = normalizedRange?.Length ?? node.Size,
            TotalSize = node.Size,
            Range = normalizedRange
        };
    }

    private async Task<(FileShare Share, FileNode Node)> GetShareAndNodeAsync(string token)
    {
        var share = await _shareRepository.FirstOrDefaultAsync(
            item => item.Token == token && item.IsEnabled);

        if (share == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterShareNotFound)
                .WithData("Token", token);
        }

        share.EnsureAccessible(_clock.Now);

        var node = await _fileNodeRepository.FindByIdAsync(
            share.FileNodeId,
            share.OwnerId,
            share.TenantId);

        if (node == null)
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterNodeNotFound)
                .WithData("Id", share.FileNodeId);
        }

        return (share, node);
    }

    private static void EnsurePassword(FileShare share, string? password)
    {
        if (!share.RequiresPassword)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterSharePasswordRequired);
        }

        if (!FileCenterSharesAppService.VerifyPassword(share, password))
        {
            throw new BusinessException(PrivateCloudDriveDomainErrorCodes.FileCenterSharePasswordInvalid);
        }
    }

    private static PublicFileShareDto ToPublicDto(
        FileShare share,
        FileNode node,
        bool passwordRequired)
    {
        return new PublicFileShareDto
        {
            Token = share.Token,
            FileNodeId = node.Id,
            FileName = node.Name,
            NodeType = node.NodeType,
            Size = node.Size,
            ContentType = node.ContentType,
            ExpirationTime = share.ExpirationTime,
            AllowDownload = share.AllowDownload,
            PasswordRequired = passwordRequired,
            VisitCount = share.VisitCount
        };
    }
}
