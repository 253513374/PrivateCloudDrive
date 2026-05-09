# ABP 代码组织约定与整改计划

> **For Hermes:** Use backend-eng / architect profiles to implement this plan task-by-task.

**Goal:** 让 PrivateCloudDrive 的接口、DTO、应用服务实现、领域对象、仓储、HTTP API、Host 集成代码按照 ABP 分层约定稳定存放，降低后续 FileCenter、MobileAuth、OpenIddict 扩展开发的维护成本。

**Architecture:** 保持当前 ABP 标准分层：`Domain.Shared` 放共享常量/枚举/错误码/本地化；`Domain` 放实体、聚合、领域服务、仓储接口；`Application.Contracts` 放远程服务接口、DTO、Input、权限定义；`Application` 放应用服务实现和应用层内部协作服务；`EntityFrameworkCore` 放 DbContext、ModelCreating、EF Repository、Migrations；`HttpApi` 放 Controller 和 HTTP 表单模型；`HttpApi.Host` 只放宿主启动、OpenIddict 扩展授权、菜单、品牌、配置。

**Tech Stack:** .NET 10, ABP 10.3.0, Entity Framework Core, OpenIddict, xUnit, Shouldly, MAUI client.

---

## 1. 当前项目结构结论

检查路径：`D:/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core`

当前 ABP 项目已具备标准分层：

| 项目 | 当前职责 | 结论 |
|---|---|---|
| `PrivateCloudDrive.Domain.Shared` | 常量、枚举、错误码、本地化、模块共享配置 | 基本符合 ABP |
| `PrivateCloudDrive.Domain` | FileCenter/MobileAuth 实体、聚合、领域服务、仓储接口 | 基本符合 ABP |
| `PrivateCloudDrive.Application.Contracts` | AppService 接口、DTO、Input、权限定义 | 基本符合 ABP |
| `PrivateCloudDrive.Application` | AppService 实现、应用层内部服务、缓存/限流/第三方身份服务 | 基本符合 ABP |
| `PrivateCloudDrive.EntityFrameworkCore` | DbContext、ModelCreating、EF Repository、Migrations | 基本符合 ABP |
| `PrivateCloudDrive.HttpApi` | Controller、HTTP multipart form model | 基本符合 ABP |
| `PrivateCloudDrive.HttpApi.Host` | Host 启动、OpenIddict extension grant、菜单、品牌 | 基本符合 ABP |

扫描结果摘要：

| 类型 | 位置 | 数量 | 问题 |
|---|---|---:|---|
| AppService 接口 | `Application.Contracts` | 9 | 无明显错位 |
| DTO/Input | `Application.Contracts` | 31 | 无明显错位 |
| AppService 实现 | `Application` | 8 | 无明显错位 |
| Entity/Aggregate | `Domain` | 6 | 无明显错位 |
| Domain Service/Manager | `Domain` | 1 | 无明显错位 |
| Repository Interface | `Domain` | 1 | 无明显错位 |
| EF Repository | `EntityFrameworkCore` | 1 | 无明显错位 |
| Controller | `HttpApi` | 13 | 无明显错位 |
| OpenIddict ExtensionGrant | `HttpApi.Host` | 2 | 符合宿主集成定位 |

---

## 2. ABP 分层放置约定

### 2.1 Domain.Shared

路径：`aspnet-core/src/PrivateCloudDrive.Domain.Shared/<Module>`

应该放：

- `*Consts.cs`
- `*Options.cs`，仅当 Application/Host/Client 都可能引用配置结构时
- 枚举：`FileNodeType`、`UploadSessionStatus`、`MediaAssetProcessStatus`
- 错误码：`PrivateCloudDriveDomainErrorCodes`
- 本地化资源、模块共享配置

不应该放：

- 实体、聚合根
- 仓储接口
- AppService 接口
- DTO/Input，除非确认为跨层共享值对象且不会暴露应用契约语义

### 2.2 Domain

路径：`aspnet-core/src/PrivateCloudDrive.Domain/<Module>`

应该放：

- 实体/聚合：`FileNode`、`FileShare`、`UploadSession`、`ExternalUserBinding`
- 领域服务/Manager：`FileNodeManager`
- 仓储接口：`IFileNodeRepository`
- 领域事件、领域规则、跨聚合不变量

不应该放：

- AppService 接口和实现
- Controller
- EF Core 实现
- HTTP/Host/OpenIddict 请求处理细节
- 面向客户端的 DTO/Input

### 2.3 Application.Contracts

路径：`aspnet-core/src/PrivateCloudDrive.Application.Contracts/<Module>`

应该放：

- `I*AppService.cs`
- `*Dto.cs`
- `*Input.cs`
- 面向前端/MAUI/远程服务的 `*ResultDto`、`*SettingsDto`
- 权限定义：`Permissions/PrivateCloudDrivePermissions.cs`、`PrivateCloudDrivePermissionDefinitionProvider.cs`
- RemoteService 常量

不应该放：

- AppService 实现
- 领域实体
- EF/DbContext/Migration
- 内部缓存项、内部票据模型、第三方 provider 内部 identity 模型

### 2.4 Application

路径：`aspnet-core/src/PrivateCloudDrive.Application/<Module>`

应该放：

- `*AppService.cs` 实现
- 应用层内部服务接口与实现，例如：
  - `IExternalLoginService`
  - `IWechatLoginService`
  - `IExternalIdentityService`
  - `IExternalAuthRateLimiter`
  - `IFileCenterBlobStorageService`
- 缓存项、票据模型、内部输入输出模型，例如：
  - `ExternalLoginInput`
  - `ExternalLoginResult`
  - `ExternalBindingTicketCacheItem`
- BackgroundJob、BlobStorage、媒体处理器等应用层协调代码

注意：Application 内部接口可以保留在 Application 项目，前提是它们不是远程服务契约，不被客户端直接引用。

### 2.5 EntityFrameworkCore

路径：`aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/<Module>`

应该放：

- `*DbContextModelCreatingExtensions.cs`
- `EfCore*Repository.cs`
- EF Core entity configuration
- Migrations

不应该放：

- 应用服务实现
- Controller
- 业务 DTO
- OpenIddict grant handler

### 2.6 HttpApi

路径：`aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/<Module>`
路径：`aspnet-core/src/PrivateCloudDrive.HttpApi/Models/<Module>`

应该放：

- Controller
- 仅服务于 HTTP 绑定的模型，例如 multipart form：
  - `UploadChunkForm`
  - `UploadSmallFileForm`
- Route、FromForm、FromBody、FileStreamResult、HTTP 状态码转换

不应该放：

- 业务实现逻辑
- 应用层内部服务
- OpenIddict token endpoint extension grant
- EF/Domain 代码

### 2.7 HttpApi.Host

路径：`aspnet-core/src/PrivateCloudDrive.HttpApi.Host`

应该放：

- `Program.cs`
- `PrivateCloudDriveHttpApiHostModule.cs`
- 菜单、品牌、Host 配置
- OpenIddict Token Extension Grant / Host 级认证事件处理：
  - `WechatTokenExtensionGrant`
  - `ExternalTokenExtensionGrant`
  - `PasswordLoginRateLimitHandlers`

不应该放：

- 普通业务 Controller
- AppService 接口/实现
- DTO/Input
- Domain/EF 代码

---

## 3. 当前发现的问题与风险

### 3.1 代码位置大体正确，但注释/Attribute 顺序存在 C# XML 文档约定问题

发现 20 个文件存在 `/// <summary>` 放在 Attribute 后面的情况，例如：

- `Application/FileCenter/FileCenterChunkUploadService.cs`
- `Application/FileCenter/FileCenterFoldersAppService.cs`
- `Application/MobileAuth/ExternalAuthAppService.cs`
- `Application.Contracts/MobileAuth/BindCurrentExternalLoginInput.cs`
- `Application.Contracts/MobileAuth/BindExistingExternalLoginInput.cs`
- `HttpApi/Controllers/FileCenter/FileCenterFilesController.cs`
- `HttpApi/Controllers/FileCenter/PublicFileSharesController.cs`
- `HttpApi/Controllers/MobileAuth/MobileAuthExternalController.cs`

风险：

- XML 文档注释不会正确绑定到目标成员。
- IDE/Swagger/文档生成可能丢失说明。
- 不影响编译，但影响代码规范和 API 文档质量。

约定：

```csharp
/// <summary>
/// 说明。
/// </summary>
[Authorize]
[Route("api/xxx")]
public class XxxController : PrivateCloudDriveController
{
}
```

而不是：

```csharp
[Authorize]
/// <summary>
/// 说明。
/// </summary>
public class XxxController : PrivateCloudDriveController
{
}
```

### 3.2 `Application` 中内部接口较多，需要明确命名边界

当前这些接口位于 `Application`，可以接受，因为它们不是远程契约：

- `IExternalAuthRateLimiter`
- `IExternalBindingTicketStore`
- `IExternalIdentityService`
- `IExternalLoginService`
- `IWechatLoginService`
- `IFileCenterBlobStorageService`
- `IFileCenterVideoProcessor`

约定：

- 如果接口供 Controller/前端/MAUI/远程客户端调用，必须放 `Application.Contracts`，命名为 `I*AppService`。
- 如果接口仅用于应用层内部 DI 协作，允许放 `Application`，但建议集中在 `<Module>/Internal` 或 `<Module>/Services` 子目录，避免与 AppService 契约混淆。

### 3.3 当前工作区存在大量未提交改动

`git status` 显示 FileCenter、MobileAuth、OpenIddict、MAUI、docker、docs 均有改动和新增文件。

整改前必须注意：

- 不要大规模移动文件，以免和正在开发的功能混在一起。
- 第一阶段只做“约定文档 + 注释顺序修正 + 小范围目录整理”。
- 涉及 namespace/文件移动的重构应单独提交。

---

## 4. 推荐目标结构

```text
aspnet-core/src
├── PrivateCloudDrive.Domain.Shared
│   ├── FileCenter
│   │   ├── FileNodeConsts.cs
│   │   ├── FileNodeType.cs
│   │   └── ...
│   └── MobileAuth
│       ├── ExternalLoginConsts.cs
│       ├── ExternalLoginOptions.cs
│       └── ...
├── PrivateCloudDrive.Domain
│   ├── FileCenter
│   │   ├── FileNode.cs
│   │   ├── FileNodeManager.cs
│   │   └── IFileNodeRepository.cs
│   └── MobileAuth
│       ├── ExternalUserBinding.cs
│       └── MobileAuthAuditLog.cs
├── PrivateCloudDrive.Application.Contracts
│   ├── FileCenter
│   │   ├── IFileCenterFoldersAppService.cs
│   │   ├── FileNodeDto.cs
│   │   └── CreateFolderInput.cs
│   ├── MobileAuth
│   │   ├── IExternalAuthAppService.cs
│   │   ├── ExternalBindingDto.cs
│   │   └── BindCurrentExternalLoginInput.cs
│   └── Permissions
├── PrivateCloudDrive.Application
│   ├── FileCenter
│   │   ├── FileCenterFoldersAppService.cs
│   │   ├── FileCenterChunkUploadService.cs
│   │   ├── Services
│   │   │   ├── IFileCenterBlobStorageService.cs
│   │   │   └── FileCenterBlobStorageService.cs
│   │   └── MediaProcessing
│   └── MobileAuth
│       ├── ExternalAuthAppService.cs
│       ├── WechatAuthAppService.cs
│       ├── Services
│       ├── RateLimiting
│       └── Tickets
├── PrivateCloudDrive.EntityFrameworkCore
│   ├── FileCenter
│   └── MobileAuth
├── PrivateCloudDrive.HttpApi
│   ├── Controllers
│   │   ├── FileCenter
│   │   └── MobileAuth
│   └── Models
│       └── FileCenter
└── PrivateCloudDrive.HttpApi.Host
    └── MobileAuth
        ├── ExternalTokenExtensionGrant.cs
        ├── WechatTokenExtensionGrant.cs
        └── PasswordLoginRateLimitHandlers.cs
```

---

## 5. 分阶段整改任务

### Task 1: 固化 ABP 代码组织文档

**Objective:** 把本文件作为项目后续开发约束。

**Files:**

- Create/Modify: `docs/abp-code-organization-plan.md`

**Steps:**

1. 阅读本文件。
2. 和团队确认“Application 内部接口允许留在 Application，但远程服务接口必须在 Application.Contracts”。
3. 后续新增模块按本文件结构执行。

**Verification:**

- 文档存在。
- 新增开发任务能引用本文件作为代码组织标准。

### Task 2: 修正 XML 注释与 Attribute 顺序

**Objective:** 让 XML 文档注释正确绑定类/方法/属性，符合 C# 和 ABP API 文档生成习惯。

**Files:**

优先处理：

- `src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/*.cs`
- `src/PrivateCloudDrive.HttpApi/Controllers/MobileAuth/MobileAuthExternalController.cs`
- `src/PrivateCloudDrive.Application/FileCenter/*.cs`
- `src/PrivateCloudDrive.Application/MobileAuth/ExternalAuthAppService.cs`
- `src/PrivateCloudDrive.Application.Contracts/MobileAuth/BindCurrentExternalLoginInput.cs`
- `src/PrivateCloudDrive.Application.Contracts/MobileAuth/BindExistingExternalLoginInput.cs`

**Rule:**

XML 注释必须在 Attribute 上方。

**Verification command:**

```bash
cd D:/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core
python - <<'PY'
from pathlib import Path
skip={'bin','obj','.vs','Migrations','node_modules'}
issues=[]
for p in Path('src').rglob('*.cs'):
    if any(part in skip for part in p.parts):
        continue
    lines=p.read_text(encoding='utf-8-sig', errors='ignore').splitlines()
    for i,l in enumerate(lines):
        if l.strip().startswith('/// <summary>'):
            j=i-1
            while j>=0 and lines[j].strip()=='' :
                j-=1
            if j>=0 and lines[j].strip().startswith('['):
                issues.append((p.as_posix(), i+1, lines[j].strip()))
                break
print('\n'.join(f'{p}:L{line} after {attr}' for p,line,attr in issues))
raise SystemExit(1 if issues else 0)
PY
```

Expected: no output and exit code 0.

### Task 3: 审查 Application 内部接口是否需要子目录归类

**Objective:** 降低 Application 目录混乱度，但避免无收益的大规模移动。

**Candidate files:**

FileCenter 内部服务：

- `FileCenterBlobStoragePathProvider.cs`
- `FileCenterBlobStorageService.cs`
- `FileCenterMediaAssetService.cs`
- `IFileCenterVideoProcessor.cs`
- `FfmpegFileCenterVideoProcessor.cs`
- `FileCenterVideoProcessingResult.cs`

MobileAuth 内部服务：

- `IExternalAuthRateLimiter.cs`
- `DistributedCacheExternalAuthRateLimiter.cs`
- `IExternalBindingTicketStore.cs`
- `DistributedCacheExternalBindingTicketStore.cs`
- `IExternalIdentityService.cs`
- `DefaultExternalIdentityService.cs`
- `IExternalLoginService.cs`
- `ExternalLoginInput.cs`
- `ExternalLoginResult.cs`
- `IWechatLoginService.cs`
- `WechatLoginInput.cs`
- `WechatLoginResult.cs`

**Recommended structure:**

- `Application/FileCenter/Services`
- `Application/FileCenter/MediaProcessing`
- `Application/MobileAuth/Services`
- `Application/MobileAuth/RateLimiting`
- `Application/MobileAuth/Tickets`

**Caution:**

- C# namespace 当前是 file-scoped namespace，移动文件本身不要求改 namespace。
- 如果项目启用了 folder-based namespace 规范，再统一 namespace；否则先不改 namespace，减少风险。

**Verification:**

```bash
dotnet build PrivateCloudDrive.slnx --no-restore
```

### Task 4: 建立新增 ABP 文件放置检查清单

**Objective:** 让后续 Codex/Cursor/Claude Code 不再把接口和实现放错项目。

**Checklist:**

- 新 AppService 接口：`Application.Contracts/<Module>/I<Xxx>AppService.cs`
- 新 AppService 实现：`Application/<Module>/<Xxx>AppService.cs`
- 新 DTO/Input：`Application.Contracts/<Module>/<Xxx>Dto.cs` 或 `<Xxx>Input.cs`
- 新 Entity/Aggregate：`Domain/<Module>/<Xxx>.cs`
- 新 Const/Enum/Options/ErrorCode：`Domain.Shared/<Module>/...`
- 新 Repository Interface：`Domain/<Module>/I<Xxx>Repository.cs`
- 新 EF Repository：`EntityFrameworkCore/<Module>/EfCore<Xxx>Repository.cs`
- 新 ModelCreating：`EntityFrameworkCore/<Module>/<Module>DbContextModelCreatingExtensions.cs`
- 新 Controller：`HttpApi/Controllers/<Module>/<Xxx>Controller.cs`
- 新 multipart/form HTTP model：`HttpApi/Models/<Module>/<Xxx>Form.cs`
- 新 OpenIddict token grant / Host auth handler：`HttpApi.Host/<Module>/<Xxx>ExtensionGrant.cs`

---

## 6. 验收标准

| 验收项 | 标准 |
|---|---|
| 分层位置 | AppService 接口、实现、DTO、实体、仓储、Controller、ExtensionGrant 均在约定项目内 |
| 编译 | `dotnet build PrivateCloudDrive.slnx --no-restore` 通过 |
| 测试 | FileCenter/MobileAuth 相关测试通过 |
| API 文档 | XML 注释在 Attribute 上方，Swagger/IDE 能识别 |
| 风险控制 | 不在同一提交混合业务逻辑重写与目录重构 |
| 可维护性 | 新增模块能按清单快速确定文件位置 |

---

## 7. 给 backend-eng 的执行提示词

```text
你是 Hermes-Backend，请在 D:/Devs/Projects/Personal/PrivateCloudDrive/aspnet-core 中执行 ABP 代码组织规范整改。

背景：
项目是 ABP 10.3.0 + .NET 10，当前分层基本正确，但需要按 docs/abp-code-organization-plan.md 固化约定，并优先修正 XML 注释与 Attribute 顺序问题。

任务目标：
1. 阅读 docs/abp-code-organization-plan.md。
2. 不做大规模业务逻辑改动。
3. 修正 src 下 XML 注释位于 Attribute 后方的问题，确保 /// <summary> 在 Attribute 上方。
4. 不移动文件，除非发现明确违反 ABP 分层的接口/实现位置。
5. 运行检查脚本确认没有 XML 注释顺序问题。
6. 运行 dotnet build PrivateCloudDrive.slnx --no-restore。

验收标准：
1. 检查脚本无输出且 exit code 为 0。
2. dotnet build 通过。
3. git diff 只包含注释顺序/规范文档/必要小范围调整，不改变业务逻辑。
4. 输出变更文件、验证结果、风险和下一步建议。
```

## 8. 给 architect 的审查提示词

```text
你是 Hermes-Architect，请审查 D:/Devs/Projects/Personal/PrivateCloudDrive/docs/abp-code-organization-plan.md 与 aspnet-core/src 当前代码结构。

重点审查：
1. 是否符合 ABP 分层依赖方向。
2. Application.Contracts 是否只包含远程契约、DTO、Input、权限定义。
3. Application 内部服务接口是否有必要移动到子目录，还是保持当前结构更利于交付。
4. OpenIddict ExtensionGrant 放在 HttpApi.Host 是否合理。
5. 是否存在会影响 MVP 交付的过度重构。

输出：
- 通过项
- 风险项
- 必须整改项
- 可延后优化项
- 最终建议：立即整改 / 分阶段整改 / 暂不移动
```
