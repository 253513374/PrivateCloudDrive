# ADR-02: 空间权限模型设计

| 元数据 | 值 |
|:------:|:---:|
| ADR 编号 | 02 |
| 标题 | 空间权限模型设计 |
| 状态 | **提案（Proposed）** |
| 日期 | 2026-07-16 |
| 负责人 | Hermes-Architect（architect） |
| 适用范围 | V2.0 MVP 空间底座权限模型 |
| 参考输入 | `docs/scenario-matrix-v2.0.md` §1.2（角色权限矩阵）、`docs/release-plan-v2.0.md` §2.3（成员管理范围）、`docs/architecture-v2.0-boundary.md` §2/§5（架构边界与必须修复规格）、现有代码结构（FileCenterFoldersAppService、FileNodeManager、IFileNodeRepository、PrivateCloudDrivePermissions） |
| 前置依赖 | ADR-01（Space 数据底座设计） |
| 下游依赖 | V2.0-1 Space 数据底座实现、V2.0-2 文件主链路接入、V2.0-3 成员与权限实现、SP-15 权限测试矩阵 |

---

## 目录

1. [背景与决策驱动](#1-背景与决策驱动)
2. [空间角色定义与权限树](#2-空间角色定义与权限树)
3. [权限继承模型](#3-权限继承模型)
4. [ABP 权限集成方案](#4-abp-权限集成方案)
5. [API 鉴权改造方案](#5-api-鉴权改造方案)
6. [越权防护策略](#6-越权防护策略)
7. [权限测试矩阵（4 角色 × 10 操作）](#7-权限测试矩阵4-角色--10-操作)
8. [API 鉴权伪代码](#8-api-鉴权伪代码)
9. [风险等级与回滚方案](#9-风险等级与回滚方案)
10. [改造步骤与验收标准](#10-改造步骤与验收标准)

---

## 1. 背景与决策驱动

### 1.1 当前状态

V1.x 模型中，数据隔离完全依赖 `TenantId + OwnerId`：

- 文件操作：所有 `FileNode` 仓储查询以 `OwnerId == 当前用户` 为唯一过滤条件
- 权限检查：ABP `[Authorize(PrivateCloudDrivePermissions.FileCenter.Xxx)]` 属性提供系统级粗粒度鉴权
- 无空间/角色概念：所有用户对自己的文件拥有完全控制权，对他人文件无任何访问权

### 1.2 决策驱动因素

| 驱动因素 | 说明 |
|---------|------|
| 空间共享 | 家庭/团队空间需要多个用户访问同一组文件，OwnerId 模式无法表达 |
| 角色分层 | 不同成员应有不同权限（Owner / Admin / Member / Viewer） |
| 越权风险 | 引入 spaceId 作为 API 参数后，必须防止参数篡改越权 |
| 与现有 ABP 权限的兼容 | 不破坏现有 `[Authorize(PrivateCloudDrivePermissions.*)]` 体系 |
| MVP 节奏 | 权限模型应避免过度设计（不作文件夹级 ACL、不作自定义角色） |

### 1.3 决策

**采用"空间固定角色模型 + SpacePermissionService 运行时校验"架构**，在现有 ABP 权限之上叠层，不修改 ABP PermissionDefinition 体系本身。

---

## 2. 空间角色定义与权限树

### 2.1 角色枚举

```csharp
// Domain.Shared / Spaces
public enum SpaceRole
{
    Owner = 0,   // 空间所有者，唯一不可转让（MVP），拥有全部权限
    Admin = 1,   // 空间管理员，可管理成员和文件，不可管理配额/删除空间
    Member = 2,  // 空间成员，可上传/下载/删除自己的文件
    Viewer = 3   // 空间查看者，只读访问
}
```

### 2.2 权限树（5 个域 × 多操作）

```
SpacePermission 体系按功能域划分，共 5 大域：
  1. 文件操作（File）
  2. 成员管理（Member）
  3. 空间设置（Settings）
  4. 配额管理（Quota）
  5. 分享操作（Share）
```

### 2.3 逐域权限矩阵

#### 2.3.1 文件操作域

| 操作 | Owner | Admin | Member | Viewer |
|------|:-----:|:-----:|:------:|:------:|
| 浏览文件列表 | ✅ | ✅ | ✅ | ✅ |
| 下载文件 | ✅ | ✅ | ✅ | ✅ |
| 上传文件 | ✅ | ✅ | ✅ | ❌ |
| 重命名/移动（自己上传） | ✅ | ✅ | ✅ | ❌ |
| 重命名/移动（他人上传） | ✅ | ✅ | ❌ | ❌ |
| 删除/软删除（自己上传） | ✅ | ✅ | ✅ | ❌ |
| 删除/软删除（他人上传） | ✅ | ✅ | ❌ | ❌ |
| 永久删除 | ✅ | ✅ | ❌ | ❌ |
| 从回收站恢复 | ✅ | ✅ | ❌ | ❌ |

#### 2.3.2 成员管理域

| 操作 | Owner | Admin | Member | Viewer |
|------|:-----:|:-----:|:------:|:------:|
| 查看成员列表 | ✅ | ✅ | ✅ | ✅ |
| 邀请成员 | ✅ | ✅ | ❌ | ❌ |
| 移除成员 | ✅ | ✅（不可移除 Owner/Admin） | ❌ | ❌ |
| 修改成员角色 | ✅ | ✅（不可修改 Owner/Admin，不可提升为 Admin） | ❌ | ❌ |
| 禁用/启用成员 | ✅ | ✅（不可禁用 Owner/Admin） | ❌ | ❌ |

#### 2.3.3 空间设置域

| 操作 | Owner | Admin | Member | Viewer |
|------|:-----:|:-----:|:------:|:------:|
| 编辑空间名称/描述 | ✅ | ✅ | ❌ | ❌ |
| 编辑空间头像 | ✅ | ✅ | ❌ | ❌ |
| 删除空间 | ✅ | ❌ | ❌ | ❌ |
| 转让空间所有权 | ❌（MVP 不支持） | ❌ | ❌ | ❌ |

#### 2.3.4 配额管理域

| 操作 | Owner | Admin | Member | Viewer |
|------|:-----:|:-----:|:------:|:------:|
| 查看空间用量 | ✅ | ✅ | ✅ | ✅ |
| 修改空间总配额 | ✅ | ❌ | ❌ | ❌ |

#### 2.3.5 分享操作域

| 操作 | Owner | Admin | Member | Viewer |
|------|:-----:|:-----:|:------:|:------:|
| 创建分享链接 | ✅ | ✅ | ✅（仅自己上传的文件） | ❌ |
| 管理/取消分享（自己创建） | ✅ | ✅ | ✅ | ❌ |
| 管理/取消分享（他人创建） | ✅ | ✅ | ❌ | ❌ |
| 访问空间内分享的文件 | ✅ | ✅ | ✅ | ✅ |

### 2.4 角色层级关系（Role Hierarchy）

```
Owner (等级 5)
  └── Admin (等级 4)
       └── Member (等级 2)
            └── Viewer (等级 1)
```

校验规则：`当前角色等级 >= 操作所需等级` 即为有权限。等级值存储于 `SpaceRole` 枚举，用于 `>=` 比较。

---

## 3. 权限继承模型

### 3.1 决策：MVP 仅做空间级权限，不做文件夹级

| 方案 | 说明 | 决策 |
|:----:|------|:----:|
| **空间级（Space-Level）** | 角色绑定到空间，空间内所有文件和文件夹统一使用该角色的固定权限矩阵 | ✅ **MVP 采用** |
| 文件夹级（Folder-Level） | 在空间内可对特定文件夹单独设置不同角色的访问权限 | ❌ **不进 MVP**（V2.2 候选） |

**理由**：

- 场景矩阵中所有 US（US-V20-01 到 US-V20-06）均只需空间级权限
- 家庭/团队场景中每个成员对整个空间的内容应有一致的角色定位
- 文件夹级 ACL 将引入显著复杂性：权限解析时需合并空间级 + 文件夹级 + 继承链
- V1 到 V2.0 迁移的关键是引入 SpaceId 维度，文件夹级 ACL 可后续在现成基座上叠加

### 3.2 继承规则

```
用户角色 = SpaceMember.Role (绑定到 Space, 不是绑定到 Folder)
空间内所有文件/文件夹继承该角色的空间级权限矩阵
```

- ✅ 简单：权限查询仅需查 `SpaceMember` 表的一次 join
- ✅ 可预测：用户在空间内任何位置操作行为一致
- ✅ 迁移友好：与现有 `OwnerId` 查询模式无概念冲突

### 3.3 显式覆盖规则（未来扩展占位）

文件夹级权限覆盖将在 V2.2 引入，其规则草案为：

```
1. 如果某文件夹存在显式权限条目，以显式条目为准
2. 如果没有显式条目，继承空间的角色默认权限
3. 子文件夹默认继承父文件夹的覆盖设置，除非有自身的显式覆盖
4. 覆盖仅可收紧（减少权限），不可放宽（超过角色的空间级权限上限）
```

> MVP 不实现上述逻辑，仅作为架构占位约束写入代码注释。

---

## 4. ABP 权限集成方案

### 4.1 架构分层

```
┌──────────────────────────────────────────────────┐
│                  Controller/API                    │
│   [Authorize(PrivateCloudDrivePermissions.XXX)]   │ ← ABP 粗粒度拦截（系统级权限）
├──────────────────────────────────────────────────┤
│              Application Service                   │
│   method(spaceId, ...) {                          │
│       _spacePermissionService.AuthorizeAsync(     │
│           spaceId, SpacePermission.FileUpload)     │ ← 空间级细粒度校验
│       // ...业务逻辑...                            │
│   }                                                │
├──────────────────────────────────────────────────┤
│            SpacePermissionService                   │
│   → 查 SpaceMemberRepository                     │
│   → 角色 ≥ 所需等级?                              │
├──────────────────────────────────────────────────┤
│            Domain / Repository                     │
│   FileNode.SpaceId IN accessibleSpaceIds          │
│   + OwnerId 备份过滤                              │
└──────────────────────────────────────────────────┘
```

### 4.2 方案对比

| 方案 | 描述 | 优点 | 缺点 | 决策 |
|:----:|------|:----:|:----:|:----:|
| **A. ABP Policy 扩展** | 自定义 `SpacePermissionHandler` 继承 `AuthorizationHandler`，在 `[Authorize(Policy = "Space:Upload")]` 中从 route/query 提取 spaceId 校验 | 对现有 `[Authorize]` 模式友好，集中化 | 需要注册所有空间权限名称为 policy；Handler 需要从 HttpContext 提取 spaceId，不适合测试 | ❌ |
| **B. SpacePermissionService 独立服务** | 注入 `ISpacePermissionService` 到每个 AppService，在方法体内部调用 `.AuthorizeAsync(spaceId, action)` | 显式控制，单元测试友好，不污染 ABP 框架层 | 每个方法需手动调用（非 AOP） | ✅ **采用** |
| **C. ABP IPermissionChecker 扩展** | 自定义 `IPermissionChecker` 实现，在检查 `PrivateCloudDrivePermissions.FileCenter.Upload` 时自动附加 spaceId 上下文 | 透明无感，与现有 `[Authorize]` 属性完全兼容 | 需要静态/ThreadLocal 传递 spaceId，多线程不安全；IF/ELSE 疯狂 | ❌ |
| **D. ABP 授权拦截器** | 利用 ABP UnitOfWork + Authorize Attribute 拦截器，自动解析方法的 `spaceId` 参数 | AOP 方式，声明式 | 过于隐式，调试困难；ABP 拦截器需额外配置 | ❌ |

### 4.3 决策理由

方案 B（SpacePermissionService 独立服务）胜出：

1. **显式优于隐式**：每方法开头调 `AuthorizeAsync`，代码即文档，新人可理解
2. **测试友好**：Mock `ISpacePermissionService` 即可测试不同角色下的行为
3. **不绑定 ABP 框架**：不依赖 HttpContext，支持后台 Job、SignalR Hub、BackgroundService 等非 HTTP 上下文中使用
4. **与现有 `[Authorize]` 共存**：现有属性拦截系统级权限（查看审计日志、管理用户等），SpacePermissionService 拦截空间级细粒度权限——两者正交叠加

### 4.4 新组件清单

| 组件 | 命名空间 | 类型 |
|:----:|---------|:----:|
| `SpaceRole` 枚举 | `PrivateCloudDrive.Domain.Shared.Spaces` | 枚举 |
| `ISpaceMemberRepository` | `PrivateCloudDrive.Domain.Spaces` | 接口 |
| `SpaceMember` 实体 | `PrivateCloudDrive.Domain.Spaces` | 聚合根（或实体） |
| `SpacePermission` 静态常量类 | `PrivateCloudDrive.Application.Contracts.Spaces` | 静态类 |
| `ISpacePermissionService` | `PrivateCloudDrive.Application.Contracts.Spaces` | 接口 |
| `SpacePermissionService` | `PrivateCloudDrive.Application.Spaces` | 实现类 |
| `SpacePermissionLocalization` (可选) | `PrivateCloudDrive.Domain.Shared.Spaces` | 本地化资源 |

### 4.5 SpacePermissionService 接口设计

```csharp
namespace PrivateCloudDrive.Spaces;

/// <summary>
/// 空间权限校验服务。
/// 提供空间内角色 >= 操作所需等级的访问控制判断。
/// 不依赖 HttpContext，所有方法通过参数传递 spaceId。
/// </summary>
public interface ISpacePermissionService
{
    /// <summary>
    /// 校验当前用户在指定空间内是否有执行指定操作的权限。
    /// 无权限时抛出 AbpAuthorizationException（最终映射为 HTTP 403）。
    /// </summary>
    Task AuthorizeAsync(Guid spaceId, SpacePermission permission);

    /// <summary>
    /// 校验当前用户在指定空间内是否有执行指定操作的权限。
    /// 返回 boolean，不抛异常。用于前端条件渲染判断。
    /// </summary>
    Task<bool> IsGrantedAsync(Guid spaceId, SpacePermission permission);

    /// <summary>
    /// 获取当前用户在指定空间中的角色。
    /// 如果用户不在空间中，返回 null。
    /// </summary>
    Task<SpaceRole?> GetRoleAsync(Guid spaceId);

    /// <summary>
    /// 获取当前用户可访问的所有空间 Id 列表。
    /// 用于仓储查询 WHERE SpaceId IN (...)。
    /// </summary>
    Task<List<Guid>> GetAccessibleSpaceIdsAsync();

    /// <summary>
    /// 获取当前用户可访问且至少拥有指定权限的空间 Id 列表。
    /// 用于前端空间选择器过滤（如：只显示有上传权限的空间）。
    /// </summary>
    Task<List<Guid>> GetAccessibleSpaceIdsAsync(SpacePermission minimumPermission);

    /// <summary>
    /// 解析默认个人空间 Id。
    /// 当 API 未传入 spaceId 时，fallback 到当前用户的 PersonalSpace。
    /// </summary>
    Task<Guid> ResolveDefaultSpaceIdAsync();
}
```

### 4.6 SpacePermission 常量定义

```csharp
namespace PrivateCloudDrive.Spaces;

/// <summary>
/// 空间内操作权限定义。
/// 每个权限关联一个最低角色等级（RequiredRoleLevel）。
/// 通过 SpaceRoleUtil 比较当前角色等级与所需等级。
/// </summary>
public static class SpacePermissions
{
    // ── 文件操作 ──
    public static readonly SpacePermission FileBrowse     = new("FileBrowse",     requiredLevel: SpaceRole.Viewer);   // 1
    public static readonly SpacePermission FileDownload   = new("FileDownload",   requiredLevel: SpaceRole.Viewer);   // 1
    public static readonly SpacePermission FileUpload     = new("FileUpload",     requiredLevel: SpaceRole.Member);   // 2
    public static readonly SpacePermission FileRenameSelf = new("FileRenameSelf", requiredLevel: SpaceRole.Member);   // 2
    public static readonly SpacePermission FileRenameAny  = new("FileRenameAny",  requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission FileDeleteSelf = new("FileDeleteSelf", requiredLevel: SpaceRole.Member);   // 2
    public static readonly SpacePermission FileDeleteAny  = new("FileDeleteAny",  requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission FilePurge      = new("FilePurge",      requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission FileRestore    = new("FileRestore",    requiredLevel: SpaceRole.Admin);    // 4

    // ── 成员管理 ──
    public static readonly SpacePermission MemberList     = new("MemberList",     requiredLevel: SpaceRole.Viewer);   // 1
    public static readonly SpacePermission MemberInvite   = new("MemberInvite",   requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission MemberRemove   = new("MemberRemove",   requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission MemberRoleSet  = new("MemberRoleSet",  requiredLevel: SpaceRole.Owner);   // 5
    public static readonly SpacePermission MemberDisable  = new("MemberDisable",  requiredLevel: SpaceRole.Admin);    // 4

    // ── 空间设置 ──
    public static readonly SpacePermission SpaceEdit     = new("SpaceEdit",      requiredLevel: SpaceRole.Admin);   // 4
    public static readonly SpacePermission SpaceDelete   = new("SpaceDelete",    requiredLevel: SpaceRole.Owner);   // 5

    // ── 配额 ──
    public static readonly SpacePermission QuotaView     = new("QuotaView",      requiredLevel: SpaceRole.Viewer);   // 1
    public static readonly SpacePermission QuotaSet      = new("QuotaSet",       requiredLevel: SpaceRole.Owner);   // 5

    // ── 分享 ──
    public static readonly SpacePermission ShareCreate    = new("ShareCreate",   requiredLevel: SpaceRole.Member);   // 2
    public static readonly SpacePermission ShareManageOwn = new("ShareManageOwn",requiredLevel: SpaceRole.Member);   // 2
    public static readonly SpacePermission ShareManageAny = new("ShareManageAny",requiredLevel: SpaceRole.Admin);    // 4
    public static readonly SpacePermission ShareAccess    = new("ShareAccess",    requiredLevel: SpaceRole.Viewer);   // 1
}

public readonly record struct SpacePermission(string Name, SpaceRole RequiredRole);
```

### 4.7 SpacePermissionService 实现核心逻辑

```csharp
namespace PrivateCloudDrive.Spaces;

public class SpacePermissionService : ISpacePermissionService
{
    private readonly ISpaceMemberRepository _spaceMemberRepository;
    private readonly ICurrentUser _currentUser;

    public async Task AuthorizeAsync(Guid spaceId, SpacePermission permission)
    {
        var role = await GetRoleAsync(spaceId);

        if (role == null)
        {
            throw new AbpAuthorizationException(
                "您不在该空间中或空间不存在");
        }

        if (role.Value.GetLevel() < permission.RequiredRole.GetLevel())
        {
            throw new AbpAuthorizationException(
                $"需要 {permission.RequiredRole} 角色才能执行此操作，" +
                $"您的当前角色为 {role.Value}");
        }
    }

    public async Task<SpaceRole?> GetRoleAsync(Guid spaceId)
    {
        var userId = _currentUser.GetId();
        var membership = await _spaceMemberRepository.FindAsync(spaceId, userId);
        return membership?.Role;
    }

    public async Task<List<Guid>> GetAccessibleSpaceIdsAsync()
    {
        var userId = _currentUser.GetId();
        return await _spaceMemberRepository.GetSpaceIdsByUserAsync(userId);
    }

    public async Task<Guid> ResolveDefaultSpaceIdAsync()
    {
        var userId = _currentUser.GetId();
        return await _spaceMemberRepository.GetPersonalSpaceIdAsync(userId);
    }
}

// 角色等级辅助
public static class SpaceRoleExtensions
{
    public static int GetLevel(this SpaceRole role) => role switch
    {
        SpaceRole.Owner  => 5,
        SpaceRole.Admin  => 4,
        SpaceRole.Member => 2,
        SpaceRole.Viewer => 1,
        _ => 0
    };
}
```

### 4.8 与现有 ABP 权限体系的交互规则

| 场景 | ABP `[Authorize]` | SpacePermissionService | 结果 |
|:----:|:-----------------:|:---------------------:|:----:|
| 系统管理员查看审计日志 | `[Authorize(GlobalAuditLog)]` | 不调用 | 通过（全局权限，不涉及空间） |
| 空间成员上传文件 | `[Authorize(FileCenter.Upload)]` | `Authorize(spaceId, FileUpload)` | 需要两者都通过 |
| 非空间成员尝试获取空间文件列表 | `[Authorize(FileCenter.View)]` | `Authorize(spaceId, FileBrowse)` | ABP 通过（用户有 View 权限），SpacePermission 拒绝（不在空间中） |
| 登录用户可以查看空间列表 | 无 `[Authorize]`（仅需认证） | `GetAccessibleSpaceIdsAsync()` | 通过认证即可 |

**关键规则**：`[Authorize(PrivateCloudDrivePermissions.Xxx)]` 和 `SpacePermissionService.AuthorizeAsync()` 是 **两者都必须通过** 的 AND 关系。一个拦截系统级权限，一个拦截空间级细粒度权限。

---

## 5. API 鉴权改造方案

### 5.1 改造原则

1. **不破坏现有 API 契约**：所有新 `spaceId` 参数均为可选（nullable `Guid?`）
2. **向后兼容**：旧客户端不传 `spaceId` → `ResolveDefaultSpaceIdAsync()` 回退到个人空间
3. **显式校验**：每个 API 方法必须通过 `SpacePermissionService.AuthorizeAsync()` 明确校验
4. **角色 + 文件归属双层判断**：先校验角色 >= 所需等级，再判断文件归属（"自己的" vs "他人的"）

### 5.2 API 变化清单

#### 5.2.1 文件列表

| 当前 | V2.0 |
|------|------|
| `GET /api/file-center/folders?parentId=...` | `GET /api/file-center/folders?spaceId={spaceId}&parentId=...` |
| 无空间校验 | 新增：`Authorize(spaceId, FileBrowse)` → 仓储增加 `SpaceId` 过滤 |

#### 5.2.2 上传

| 当前 | V2.0 |
|------|------|
| `POST /api/file-center/upload` | `POST /api/file-center/upload?spaceId={spaceId}` |
| 无空间校验 | 新增：`Authorize(spaceId, FileUpload)` + 配额校验 `QuotaView` |

#### 5.2.3 下载

| 当前 | V2.0 |
|------|------|
| `GET /api/file-center/folders/{id}/download` | `GET /api/file-center/folders/{id}/download?spaceId={spaceId}` |
| 无空间校验 | 新增：`Authorize(spaceId, FileDownload)` |

#### 5.2.4 删除

| 当前 | V2.0 |
|------|------|
| `DELETE /api/file-center/folders/{id}` | `DELETE /api/file-center/folders/{id}?spaceId={spaceId}` |
| 无空间校验 | 新增：先 `Authorize(spaceId, FileDeleteSelf/Any)` 再根据文件归属判断 |

#### 5.2.5 新增空间管理端 API

| 端点 | 方法 | 权限校验 |
|------|:----:|:--------:|
| `/api/spaces` | POST | ABP `[Authorize(FileCenter.Manage)]` + `SpacePermission` 无需空间校验（创建时尚未有空间） |
| `/api/spaces/{spaceId}` | GET | `Authorize(spaceId, FileBrowse)` |
| `/api/spaces/{spaceId}` | PUT | `Authorize(spaceId, SpaceEdit)` |
| `/api/spaces/{spaceId}` | DELETE | `Authorize(spaceId, SpaceDelete)` |
| `/api/spaces/{spaceId}/members` | GET | `Authorize(spaceId, MemberList)` |
| `/api/spaces/{spaceId}/members` | POST | `Authorize(spaceId, MemberInvite)` |
| `/api/spaces/{spaceId}/members/{userId}` | DELETE | `Authorize(spaceId, MemberRemove)` + 检查是否尝试移除 Owner/Admin |
| `/api/spaces/{spaceId}/members/{userId}/role` | PUT | `Authorize(spaceId, MemberRoleSet)` + 检查角色变更合法性 |
| `/api/spaces/{spaceId}/quota` | GET | `Authorize(spaceId, QuotaView)` |
| `/api/spaces/{spaceId}/quota` | PUT | `Authorize(spaceId, QuotaSet)` |

### 5.3 FileNodeManager 改造要点

`FileNodeManager.GetOwnerFolderAsync()` 等方法的签名需升级：

```csharp
// V1.x 当前：通过 TenantId + OwnerId 校验文件归属
public virtual async Task<FileNode> GetOwnerFolderAsync(
    Guid? tenantId, Guid ownerId, Guid id)

// V2.0 改造：通过 TenantId + SpaceId + OwnerId 校验
// OwnerId 改为从 _currentUser 自动获取，不再通过参数传递
public virtual async Task<FileNode> GetSpaceNodeAsync(
    Guid? tenantId, Guid spaceId, Guid id)
{
    var node = await _fileNodeRepository.FindAsync(id);

    if (node == null || node.TenantId != tenantId || node.SpaceId != spaceId)
    {
        throw new BusinessException(FileCenterNodeNotFound)
            .WithData("Id", id);
    }

    EnsureFolderNode(node);
    return node;
}
```

> 注意：FileNodeManager 注入 `ICurrentUser` 或由调用方传入 userId。推荐由调用方传入以保持 Domain 层无依赖。

### 5.4 IFileNodeRepository 查询改造

```csharp
// V2.0 新增/重载仓储方法
public interface IFileNodeRepository
{
    // ── 空间感知查询（新增） ──
    Task<List<FileNode>> GetChildrenBySpaceAsync(
        Guid spaceId,
        Guid? parentId,
        int skipCount,
        int maxResultCount,
        Guid? tenantId = null,
        /* 其他筛选参数保持不变 */);

    Task<long> GetChildrenCountBySpaceAsync(
        Guid spaceId,
        Guid? parentId,
        Guid? tenantId = null,
        /* 其他筛选参数保持不变 */);

    // ── 原有方法重载（向后兼容） ──
    // 保留所有现有方法签名，内部逻辑自动适配：
    // 如果 FileNode.SpaceId == defaultSpaceId，按原有 OwnerId 查询
    // 以便 V1.x 旧客户端仍可使用
}
```

### 5.5 方法级改造对照（FileCenterFoldersAppService）

| V1.x 方法 | V2.0 改造内容 |
|-----------|--------------|
| `CreateAsync(CreateFolderInput)` | 增加 `spaceId` 参数；开头 `AuthorizeAsync(spaceId, FileUpload)`；`CreateFolderAsync` 增加 `spaceId` 参数 |
| `GetListAsync(GetFolderChildrenInput)` | 增加 `spaceId` 参数；开头 `AuthorizeAsync(spaceId, FileBrowse)`；仓储改为 `GetChildrenBySpaceAsync` |
| `RenameAsync(Guid id, RenameInput)` | 增加 `spaceId` 参数；先 `AuthorizeAsync(spaceId, FileRenameSelf/Any)` ，再判断文件归属 |
| `MoveAsync(Guid id, MoveInput)` | 同上 |
| `DeleteAsync(Guid id)` | 增加 `spaceId` 参数；先 `AuthorizeAsync(spaceId, FileDeleteSelf/Any)` ，再判断文件归属 |
| `DeleteManyAsync(BatchInput)` | 同上 |
| `RestoreAsync(Guid id)` | 增加 `spaceId` 参数；`AuthorizeAsync(spaceId, FileRestore)` |
| `HardDeleteAsync(Guid id)` | 增加 `spaceId` 参数；`AuthorizeAsync(spaceId, FilePurge)` |
| `GetDeletedListAsync(...)` | 增加 `spaceId` 参数；`AuthorizeAsync(spaceId, FileBrowse)` → 仓库查询按 `SpaceId` 过滤回收站 |

---

## 6. 越权防护策略

### 6.1 威胁模型

| 威胁类型 | 攻击向量 | 风险等级 | 影响 |
|:--------:|---------|:--------:|:----:|
| **IDOR**（不安全的直接对象引用） | 篡改 `spaceId` 参数访问其他空间文件 | 🔴 **高** | 跨空间数据泄露 |
| **IDOR** | 篡改 `fileNodeId` 参数访问其他空间的文件节点 | 🔴 **高** | 跨空间文件泄露 |
| **权限提升** | Member 尝试删除他人上传的文件 | 🟡 **中** | 数据被不当删除 |
| **权限提升** | Admin 尝试提升自己或他人为 Owner | 🟡 **中** | 越权获得空间控制权 |
| **权限提升** | Admin 尝试修改其他 Admin 的角色 | 🟡 **中** | 越权管理同级 |
| **重放攻击** | 使用旧令牌操作已退出/被移除的空间 | 🟡 **中** | 权限撤销不生效 |
| **水平越权** | 普通成员尝试访问成员管理 API | 🟡 **中** | 成员列表泄露 |
| **配额绕过** | 上传请求直接绕过配额校验 | 🟢 **低** | 超额存储 |

### 6.2 各层级防护措施

```
Layer 1: 传输层 → HTTPS 强制（已存在）
Layer 2: 认证层 → OpenIddict Bearer Token（已存在，复用）
Layer 3: ABP 属性鉴权 → [Authorize] 粗粒度（已存在，不变）
Layer 4: 空间级细粒度 → SpacePermissionService（新增）
Layer 5: 文件归属校验 → SpaceId 仓储过滤（新增）
Layer 6: 业务规则层 → 领域服务不变量（新增/改造）
Layer 7: 审计层 → OperationLog（已存在，已扩展 SpaceId）
```

### 6.3 具体防护规则

#### 规则 1：SpaceId 篡改防护

```
每个接收 spaceId 参数的 API 必须：
  1. 调用 SpacePermissionService.AuthorizeAsync(spaceId, permission)
  2. 仓储查询 WHERE FileNode.SpaceId = spaceId（双重校验）
  3. 永远不信任客户端传递的 spaceId，每次请求都重新查 SpaceMember 表
```

#### 规则 2：FileNodeId 跨空间防护

```
FileNodeManager 中所有 get/verify 操作：
  验证 node.SpaceId == requestSpaceId
  不满足 → 返回 404 (FileCenterNodeNotFound)，不泄露文件存在性
```

#### 规则 3：角色变更防提升

```
SpaceMemberAppService 中角色变更方法：
  - 如果操作者角色为 Owner → 允许所有合法变更（不可将他人改为 Owner）
  - 如果操作者角色为 Admin → 仅允许：
      a) 目标角色 不可为 Admin (角色提升保护)
      b) 目标用户 不可为 Admin 或 Owner (同级保护)
      c) 目标角色 不可超过 Admin （上限）
  - 如果操作者角色低于 Admin → 拒绝所有角色变更
    实现方式：先校验空间权限 (MemberRoleSet)，再校验具体变更规则
```

#### 规则 4：文件归属双路径校验

```
对于涉及"自己 vs 他人"文件的操作（删除、重命名）：
  1. 先校验角色 >= 操作所需最低角色（如 FileDeleteSelf 需要 Member）
  2. 再判断 node.UploaderId (或 OwnerId) == currentUser.Id
     - 是自己的文件 → FileDeleteSelf 即足够
     - 是别人的文件 → 需要 FileDeleteAny (Admin+)
  3. 两步都通过才允许操作
```

#### 规则 5：权限即时性

```
SpaceMember 变更（移除/角色变更/禁用）后：
  - 不依赖缓存（V2.0 MVP 不做 SpaceMember 缓存）
  - 每次请求都查数据库
  - 权限即时生效，不需要 Token 刷新
  - 如果将来引入缓存，变更时必须 invalidate 该用户在空间的缓存条目
```

#### 规则 6：API 参数校验清单

| 参数 | 校验规则 |
|:----:|---------|
| `spaceId` (路由/查询参数) | 必须为有效的 UUID 格式；后端查 ISpaceMemberRepository 确认用户在该空间中 |
| `fileNodeId` | 必须为有效的 UUID 格式；返回该节点时必须校验 `node.SpaceId == 请求的 spaceId` |
| `userId` (成员管理) | 不能等于当前用户（禁止自操作）；不能是 Owner（禁止操作空间创建者） |
| `role` (角色变更) | 必须为有效的 SpaceRole 枚举值；不允许变更成 Owner；Admin 不允许变更成 Admin |
| `quotaBytes` (配额设置) | 必须 > 0；必须 ≤ 系统最大值；不允许多次重复设置相同值 |

### 6.4 响应策略对照

| 越权类型 | HTTP 状态码 | 响应体 | 日志记录 |
|:--------:|:----------:|--------|:--------:|
| 空间不存在/用户不在空间中 | **404 Not Found** | `"空间不存在或您无权访问"` | 记录 UserId + SpaceId |
| 权限不足（角色 < 所需） | **403 Forbidden** | `"权限不足，需要 {角色名} 角色"` | 记录 UserId + SpaceId + 当前角色 + 所需角色 |
| 非文件所有者操作他人文件 | **403 Forbidden** | `"您无权操作其他成员上传的文件"` | 记录 UserId + SpaceId + FileNodeId |
| 角色变更越权（Admin 提级） | **403 Forbidden** | `"您无权分配 Administrator 角色"` | 记录 UserId + SpaceId + 请求的目标角色 |
| 令牌过期/无效 | **401 Unauthorized** | `"登录已过期，请重新登录"` | 标准认证日志 |
| 配额超限 | **403 / 409 Conflict** | `"空间容量不足"` | 记录 UserId + SpaceId + 配额详情 |

---

## 7. 权限测试矩阵（4 角色 × 10 操作）

### 7.1 全量测试矩阵

| 操作 | 测试描述 | Owner (5) | Admin (4) | Member (2) | Viewer (1) |
|------|---------|:---------:|:---------:|:----------:|:----------:|
| **F01** 浏览文件列表 | GET /api/spaces/{id}/files | ✅ | ✅ | ✅ | ✅ |
| **F02** 下载文件 | GET /api/spaces/{id}/files/{fid}/download | ✅ | ✅ | ✅ | ✅ |
| **F03** 上传文件 | POST /api/spaces/{id}/files/upload | ✅ | ✅ | ✅ | ❌ |
| **F04** 重命名/移动（自己文件） | PUT /api/spaces/{id}/files/{fid}/rename | ✅ | ✅ | ✅ | ❌ |
| **F05** 重命名/移动（他人文件） | PUT /api/spaces/{id}/files/{fid}/rename | ✅ | ✅ | ❌ | ❌ |
| **F06** 删除/恢复（自己文件） | DELETE /api/spaces/{id}/files/{fid} | ✅ | ✅ | ✅ | ❌ |
| **F07** 删除/恢复（他人文件） | DELETE /api/spaces/{id}/files/{fid} | ✅ | ✅ | ❌ | ❌ |
| **F08** 永久删除 | DELETE /api/spaces/{id}/files/{fid}/hard | ✅ | ✅ | ❌ | ❌ |
| **F09** 创建分享链接 | POST /api/spaces/{id}/shares | ✅ | ✅ | ✅（仅自己） | ❌ |
| **F10** 管理他人分享 | DELETE /api/spaces/{id}/shares/{sid} | ✅ | ✅ | ❌ | ❌ |
| **M01** 查看成员列表 | GET /api/spaces/{id}/members | ✅ | ✅ | ✅ | ✅ |
| **M02** 邀请成员 | POST /api/spaces/{id}/members | ✅ | ✅ | ❌ | ❌ |
| **M03** 移除成员 | DELETE /api/spaces/{id}/members/{uid} | ✅ | ✅（受限*） | ❌ | ❌ |
| **M04** 修改成员角色 | PUT /api/spaces/{id}/members/{uid}/role | ✅ | ✅（受限*） | ❌ | ❌ |
| **M05** 禁用/启用成员 | PATCH /api/spaces/{id}/members/{uid}/toggle | ✅ | ✅（受限*） | ❌ | ❌ |
| **S01** 编辑空间名称 | PUT /api/spaces/{id} | ✅ | ✅ | ❌ | ❌ |
| **S02** 删除空间 | DELETE /api/spaces/{id} | ✅ | ❌ | ❌ | ❌ |
| **Q01** 查看空间用量 | GET /api/spaces/{id}/quota | ✅ | ✅ | ✅ | ✅ |
| **Q02** 修改空间配额 | PUT /api/spaces/{id}/quota | ✅ | ❌ | ❌ | ❌ |
| **P01** 反越权测试：篡改 spaceId | 将 spaceId 改为其他空间的 UUID | ❌ | ❌ | ❌ | ❌ |
| **P02** 反越权测试：篡改 fileNodeId | 将 fileNodeId 改为其他空间文件的 UUID | ❌ | ❌ | ❌ | ❌ |
| **P03** 反越权测试：搜索跨空间 | 搜索关键词命中其他空间同名文件 | 不返回 | 不返回 | 不返回 | 不返回 |
| **P04** 反越权测试：管理员提级 | Admin 尝试将 Member 升级为 Admin | ❌ | ❌ | ❌ | ❌ |
| **P05** 反越权测试：被移除后访问 | 删除 SpaceMember 记录后尝试访问空间 | ❌ | ❌ | ❌ | ❌ |

> ✅ = 预期通过；❌ = 预期拒绝（403 或 404）
>
> 受限* = Admin 可执行但受业务规则约束（不可操作 Owner/Admin，不可提升为 Admin）

### 7.2 测试场景设计

每条测试包含：

1. **前置条件**：创建 N 个空间、M 个用户、分配角色
2. **操作**：调用被测试 API
3. **断言**：
   - 预期通过：HTTP 2xx + 正确返回内容
   - 预期拒绝：HTTP 403/404 + 错误消息
4. **后置清理**：删除测试数据

### 7.3 测试数据布局（针对 SP-15）

```
UserA → Space1 (Owner) → 文件 A-1, A-2
UserB → Space1 (Admin) → 文件 B-1
UserC → Space1 (Member) → 文件 C-1
UserD → Space1 (Viewer) → 文件 D-1
UserB → Space2 (Owner) → 文件 E-1（UserB 在 Space2 是 Owner）
UserX → (不在任何空间，仅个人空间) → 文件 X-1

测试覆盖：
  - 空间内角色差异 → F01-F10
  - 跨空间访问 → P01-P03（UserC 尝试访问 Space2 的文件）
  - 角色变更 → M03-M04（Admin 尝试修改 Admin）
  - 退出后访问 → P05（移除 UserC 后请求 Space1 文件）
```

---

## 8. API 鉴权伪代码

### 8.1 文件列表（带空间参数）

```csharp
/// <summary>
/// GET /api/spaces/{spaceId}/files 或
/// GET /api/file-center/folders?spaceId={spaceId}
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.View)]
public virtual async Task<PagedResultDto<FileNodeDto>> GetListAsync(
    Guid spaceId,
    GetFolderChildrenInput input)
{
    // Step 1: 空间级权限校验
    await _spacePermissionService.AuthorizeAsync(spaceId, SpacePermissions.FileBrowse);

    // Step 2: 解析空间上下文（获取访问者角色等）
    var role = await _spacePermissionService.GetRoleAsync(spaceId);

    // Step 3: 空间感知查询
    var totalCount = await _fileNodeRepository.GetChildrenCountBySpaceAsync(
        spaceId, input.ParentId, CurrentTenant.Id,
        input.TagId, input.IsFavorite, input.SearchKeyword,
        input.SearchScope, input.NodeType, input.MediaType);

    var items = await _fileNodeRepository.GetChildrenBySpaceAsync(
        spaceId, input.ParentId,
        input.SkipCount, input.MaxResultCount, CurrentTenant.Id,
        tagId: input.TagId, isFavorite: input.IsFavorite,
        searchKeyword: input.SearchKeyword, searchScope: input.SearchScope,
        nodeType: input.NodeType, mediaType: input.MediaType,
        sorting: input.Sorting);

    // Step 4: DTO 级权限裁剪（如果需要）
    // 例如，Member 不能看到其他文件的 UploaderId 之外的敏感信息
    // MVP 阶段不做此项裁剪，仅返回全部节点信息

    return new PagedResultDto<FileNodeDto>(
        totalCount,
        items.Select(ToDto).ToList());
}
```

### 8.2 上传文件（文件归属 + 配额校验）

```csharp
/// <summary>
/// POST /api/file-center/upload?spaceId={spaceId}
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Upload)]
public virtual async Task<FileNodeDto> UploadAsync(
    Guid spaceId,
    UploadFileInput input)
{
    // Step 1: 空间权限校验（可上传）
    await _spacePermissionService.AuthorizeAsync(spaceId, SpacePermissions.FileUpload);

    // Step 2: 空间配额校验（查看用量）
    var spaceQuota = await _spaceQuotaRepository.GetBySpaceIdAsync(spaceId);
    if (spaceQuota.UsedBytes + input.Size > spaceQuota.QuotaBytes)
    {
        throw new BusinessException(SpaceQuotaExceeded)
            .WithData("Available", spaceQuota.QuotaBytes - spaceQuota.UsedBytes);
    }

    // Step 3: 创建文件节点（记录 SpaceId）
    var ownerId = CurrentUser.GetId();
    var file = await _fileNodeManager.CreateFileAsync(
        CurrentTenant.Id, ownerId, spaceId, input.ParentId, 
        input.Name, input.Size, input.ContentType, input.BlobName);

    await _fileNodeRepository.InsertAsync(file, autoSave: true);

    // Step 4: 更新空间用量
    await _spaceQuotaRepository.IncrementUsedBytesAsync(spaceId, input.Size);

    // Step 5: 审计日志记录（含 SpaceId）
    await _operationLogManager.LogAsync(
        spaceId: spaceId,
        action: "FileUpload",
        operatorId: ownerId,
        operatorRole: role,
        targetId: file.Id);

    return ToDto(file);
}
```

### 8.3 删除文件（自己 vs 他人）

```csharp
/// <summary>
/// DELETE /api/spaces/{spaceId}/files/{fileNodeId}
/// </summary>
[Authorize(PrivateCloudDrivePermissions.FileCenter.Delete)]
public virtual async Task DeleteAsync(Guid spaceId, Guid fileNodeId)
{
    var ownerId = CurrentUser.GetId();

    // Step 1: 获取文件节点并校验空间归属
    var node = await _fileNodeManager.GetSpaceNodeAsync(
        CurrentTenant.Id, spaceId, fileNodeId);

    // Step 2: 判断文件归属，选择所需权限
    var isOwnFile = node.OwnerId == ownerId;
    var requiredPermission = isOwnFile
        ? SpacePermissions.FileDeleteSelf   // Member (2)
        : SpacePermissions.FileDeleteAny;   // Admin (4)

    // Step 3: 空间级权限校验
    await _spacePermissionService.AuthorizeAsync(spaceId, requiredPermission);

    // Step 4: 执行删除（委托给 FileNodeManager 处理级联删除）
    if (node.NodeType == FileNodeType.Folder)
    {
        await _fileNodeManager.DeleteFolderTreeAsync(
            CurrentTenant.Id, ownerId, node);
    }
    else
    {
        await _fileNodeRepository.DeleteAsync(node);
    }
}
```

### 8.4 索引查询的权限过滤（仓储层）

```csharp
// EfCoreFileNodeRepository.cs
public async Task<List<FileNode>> GetChildrenBySpaceAsync(
    Guid spaceId, Guid? parentId, ...)
{
    var query = await GetQueryableAsync();

    // 空间隔离：强制 SpaceId 过滤
    query = query.Where(n => n.SpaceId == spaceId);

    // 软删除过滤
    if (!includeDeleted)
        query = query.Where(n => !n.IsDeleted);

    // 原有其他筛选条件
    if (parentId.HasValue)
        query = query.Where(n => n.ParentId == parentId.Value);
    else
        query = query.Where(n => n.ParentId == null);

    // ... 其他筛选（tagId, isFavorite, searchKeyword, mediaType 等）

    return await _asyncExecuter.ToListAsync(query);
}
```

### 8.5 空间切换时的认证流

```csharp
/// <summary>
/// MAUI 切换到某空间时调用，验证用户在当前空间中的角色
/// 非严格鉴权，用于前端初始化 UI 状态
/// </summary>
/// <returns>空间详情 + 当前用户角色 + 可用操作列表</returns>
/// GET /api/spaces/{spaceId}/switch
[Authorize]
public virtual async Task<SpaceSwitchDto> SwitchToSpaceAsync(Guid spaceId)
{
    var role = await _spacePermissionService.GetRoleAsync(spaceId);

    if (role == null)
    {
        // 用户不在空间中 → 返回 403（不同于文件查询用 404）
        throw new AbpAuthorizationException("您无权访问该空间");
    }

    var space = await _spaceRepository.GetAsync(spaceId);

    return new SpaceSwitchDto
    {
        Id = space.Id,
        Name = space.Name,
        CurrentUserRole = role.Value,
        CanUpload = role.Value.GetLevel() >= SpaceRole.Member.GetLevel(),
        CanManageMembers = role.Value.GetLevel() >= SpaceRole.Admin.GetLevel(),
        CanManageQuota = role.Value == SpaceRole.Owner,
        UsedBytes = space.Quota.UsedBytes,
        QuotaBytes = space.Quota.QuotaBytes
    };
}
```

---

## 9. 风险等级与回滚方案

### 9.1 风险登记册

| 风险 | 等级 | 概率 | 影响 | 应对措施 |
|:----:|:----:|:----:|:----:|:--------|
| **R1**: 空间权限校验遗漏某 API 端点 | 🔴 高 | 中 | 越权泄露 | 强制 code review + 权限测试矩阵全覆盖 |
| **R2**: 仓储查询未添加 SpaceId 过滤 | 🔴 高 | 中 | 跨空间返回数据 | EF Core 集成测试 + 测试矩阵 P01-P03 |
| **R3**: 旧客户端不传 spaceId 时权限判断错误 | 🟡 中 | 低 | 个人空间访问异常 | `ResolveDefaultSpaceIdAsync` 始终返回正确默认空间 |
| **R4**: 管理员角色变更逻辑中遗漏同级保护 | 🟡 中 | 中 | Admin 可降级其他 Admin | 单元测试覆盖 M03-M04 场景 |
| **R5**: 文件归属判断（自己 vs 他人）逻辑错误 | 🟡 中 | 低 | 成员可删除他人文件 | F06/F07 测试矩阵覆盖 |
| **R6**: 空间删除后未清空成员关系 | 🟢 低 | 低 | 空间重建后旧成员自动获得访问 | EF 级联删除 + 迁移脚本 |

### 9.2 回滚方案

| 场景 | 回滚操作 | 影响范围 | 恢复时长 |
|:----:|---------|:--------:|:--------:|
| **R1/R2 越权泄漏** | 立即关闭 V2.0 功能入口（feature flag）；保留 `OwnerId` 查询兼容路径 | V2.0 空间功能不可用，个人云盘无影响 | 5 min（feature flag）/ 30 min（回滚代码） |
| **R3 旧客户端异常** | 修正 `ResolveDefaultSpaceIdAsync` 实现（不涉及数据回滚） | 仅旧客户端受影响 | 按 PR 修复部署 |
| **R4/R5 逻辑 Bug** | 提交热修复 PR，不涉及数据回滚 | 无数据影响 | 按 Hotfix 流程 |
| **DB 迁移失败** | 回滚到 V1.4 代码基线 + 恢复迁移前 DB dump | 全服务停机，数据无损 | 15-30 min |

### 9.3 降级兼容策略

```
┌──────────────────────────────────────┐
│  API 兼容层                           │
│                                      │
│  新客户端: spaceId = explicit         │
│      → SpacePermissionService 校验    │
│      → 仓储: SpaceId IN (...)         │
│                                      │
│  旧客户端: spaceId = null             │
│      → ResolveDefaultSpaceIdAsync()   │
│      → OwnerId 过滤 (兼容 V1.x)       │
│      → 只操作个人默认空间             │
└──────────────────────────────────────┘
```

---

## 10. 改造步骤与验收标准

### 10.1 实施步骤（按顺序）

| 步骤 | 描述 | 负责人 | 人天 |
|:----:|------|:-----:|:----:|
| 1 | 定义 `SpaceRole` 枚举 + `SpacePermission` 常量 | backend-eng | 0.5 |
| 2 | 定义 `ISpacePermissionService` 接口 + `SpacePermissionService` 实现 | backend-eng | 1 |
| 3 | 定义 `ISpaceMemberRepository` 扩展方法（GetRoleAsync, GetAccessibleSpaceIdsAsync, ResolveDefaultSpaceIdAsync） | backend-eng | 1 |
| 4 | 改造 `FileNodeManager`：增加 `GetSpaceNodeAsync` 方法 + SpaceId 校验 | backend-eng | 1 |
| 5 | 改造 `IFileNodeRepository`：增加 `GetChildrenBySpaceAsync` 等空间感知方法 | backend-eng | 2 |
| 6 | 改造 `FileCenterFoldersAppService`：每个方法增加 spaceId 参数 + SpacePermissionService 校验 | backend-eng | 3 |
| 7 | 改造 `FileCenterSharesAppService`：分享操作增加空间权限校验 | backend-eng | 1 |
| 8 | 改造文件上传流程（`FileCenterFileUploadService` + `FileCenterChunkUploadService`）：增加 spaceId 和配额校验 | backend-eng | 2 |
| 9 | 新建 `SpaceMemberAppService`：成员管理 API（CRUD + 角色变更 + 业务规则保护） | backend-eng | 2 |
| 10 | 越权防护审计（code review + 安全扫描） | architect + backend-eng | 1 |
| 11 | 权限测试矩阵编码 + 执行（全量 24 场景） | qa-eng / backend-eng | 3 |
| 12 | 回归测试（270+ 现有测试 + 新测试） | qa-eng / backend-eng | 1 |

**合计**：约 18.5 人天

### 10.2 验收标准

| 验收项 | 方法 | 预期 |
|--------|------|------|
| AC-01 | Owner 在空间内执行所有操作 | 全部通过 |
| AC-02 | Admin 执行文件操作 + 成员管理（受限） | 符合权限矩阵 |
| AC-03 | Member 执行文件操作（不可删除他人的） | 符合权限矩阵 |
| AC-04 | Viewer 仅浏览和下载 | 上传/删除/分享均 403 |
| AC-05 | 篡改 spaceId 参数 | 404 或 403 |
| AC-06 | 篡改 fileNodeId 参数跨空间 | 404 |
| AC-07 | Admin 尝试提升 Member 为 Admin | 403 |
| AC-08 | 被移除空间成员访问空间 | 404 |
| AC-09 | 空间内搜索不返回其他空间文件 | 搜索结果隔离 |
| AC-10 | 不传 spaceId 的旧客户端正常使用 | 默认个人空间可用 |
| AC-11 | 270+ 现有后端测试全部 PASS | 0 回归失败 |
| AC-12 | 新增空间权限单元测试覆盖全部 4 角色 × 10 操作 | 全部 PASS |

---

## 附录 A：SpacePermission 与现有 PrivateCloudDrivePermissions 对照

| 空间内操作 | 对应 SpacePermission | 相关 ABP 权限 | 关系 |
|-----------|:--------------------:|:------------:|:----:|
| 浏览文件 | `FileBrowse` | `FileCenter.View` | AND（两者都需要） |
| 上传文件 | `FileUpload` | `FileCenter.Upload` | AND |
| 下载文件 | `FileDownload` | `FileCenter.Download` | AND |
| 删除文件 | `FileDeleteSelf/Any` | `FileCenter.Delete` | AND |
| 创建分享 | `ShareCreate` | `FileCenter.Share` | AND |
| 管理标签 | —（空间级不隔离标签） | `FileCenter.Tags` | 仅 ABP，空间内标签可复用 |
| 空间管理 | `SpaceEdit/SpaceDelete` | `FileCenter.Manage` | AND |

## 附录 B：关键决策日志

| 决策 ID | 决策 | 理由 | 替代方案 |
|:-------:|------|------|:--------:|
| D-01 | 空间角色固定 4 级（Owner/Admin/Member/Viewer） | 家庭/团队场景 95% 覆盖，避免 ACL 过度设计 | 自定义角色（V2.2） |
| D-02 | MVP 不做文件夹级权限 | 用户故事全为空间级，文件夹级增加 10x 复杂度 | 文件夹 ACL（V2.2） |
| D-03 | SpacePermissionService 独立服务（非 ABP Policy 扩展） | 测试友好、非 HTTP 依赖、显式调用 | ABP Policy Handler |
| D-04 | ABP `[Authorize]` + SpacePermissionService 双重 AND 校验 | 系统级 + 空间级权限正交分离 | 完全替换 ABP 权限 |
| D-05 | 不缓存 SpaceMember 查询（每次请求查 DB） | MVP 权限即时性高于性能优化；用户数少（≤ 20/空间） | Redis 缓存（V2.x） |
| D-06 | 无空间参数时 fallback 到默认个人空间 | 向后兼容旧客户端 | 强制要求 spaceId（破坏 V1.x 兼容） |
| D-07 | 成员不存在的空间返回 404（非 403） | 不泄露空间存在的敏感信息 | 统一 403（MVDA 权衡后选 404） |
