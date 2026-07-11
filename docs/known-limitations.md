# Known Limitations / 已知限制

本文列出 PrivateCloudDrive 当前公开文档和 D7 发布闸门必须一致说明的限制。它不是缺陷列表，而是发布范围边界。

## 1. 发布状态

当前 Private Backup MVP 仍处于内部 RC / evidence hardening 阶段。根据 `docs/release-gate-d7-decision-2026-05-26.md`，在 main CI/Security Gate、安全门禁、Android 最终证据包和 issue 收口完成前，不升级用户最终验收，不发布 Public RC。

## 2. 隐私与加密

- 当前 MVP 不承诺端到端加密或零知识加密。
- 部署管理员、数据库/存储/备份介质访问者可能接触原始文件或元数据。
- App、公开文档和验收证据不得暴露 token、cookie、AppSecret、AccessKey、连接串、`.env` 原文、完整私有 URL、真实 bucket/object key 或用户真实隐私内容。

## 3. 存储与恢复

- 默认可交付存储路径是 FileSystem / Docker storage volume。
- 默认备份恢复覆盖 PostgreSQL 元数据和 FileCenter storage volume。
- Aliyun OSS bucket/object 不由默认 `storage.tar.gz` 覆盖，需部署者独立启用云侧版本控制、复制、生命周期或对象备份。
- MinIO profile 当前不应描述为已交付的 FileCenter Provider 主路径；保持 Not Now 或实验/可选服务口径。
- 手机本地缓存不能单独恢复服务器文件。
- 生产恢复前必须先 dry-run；破坏性恢复只允许一次性测试栈或明确授权的目标栈。

## 4. 客户端与平台

- D7 最低发布证据以 Android 手机优先链路为核心；iOS/真实设备全量矩阵按发布范围继续回填。
- Android Debug APK 手工验收建议启用 `EmbedAssembliesIntoApk=true` 并关闭 Fast Deployment 依赖。
- 微信登录依赖真实微信开放平台移动应用、Android 包名/签名、iOS Bundle/URL Scheme、后端 AppSecret 和安装微信的真机；默认不作为 Private Backup D7 必备项。
- 软键盘遮挡、系统文件选择器、视频播放和平台安全区仍需在目标设备补验。

## 5. 功能范围 Not Now

- NAS OS、RAID、磁盘池、SMB/NFS。
- 桌面同步客户端和后台自动相册备份。
- AI 相册、AI 搜索、OCR、人物识别。
- 多节点高可用、企业审批、Office 在线协作。
- App 内一键恢复服务器。
- 自动云侧对象存储灾备编排。

## 6. 发布前必须复核

| 检查 | 通过条件 |
| --- | --- |
| CI / Security Gate | main 最新 workflow 绿灯，或存在 release-manager 可审计豁免 |
| 安全扫描 | secret/log scan 对发布范围无未解释 finding |
| Android 证据包 | 单一报告覆盖登录、备份、失败重试、下载/预览、删除/恢复、容量/健康、恢复/隐私边界 |
| 文档一致性 | README、deployment、testing、DR、release notes 与本文件口径一致 |
| Issue/PR | 发布主线 issue 和安全 PR 已合并、关闭或裁剪为非阻断 |

## 7. V1.3 已知限制

以下限制在 V1.3 / V1.3b 中已知且不会被修复，在评估部署和使用时请充分了解（来源：`docs/release-notes-v1.3.md` §2）。

| 编号 | 限制 | 影响 | 规避/备注 |
|:----:|------|------|-----------|
| KN-V1.3-01 | 禁用用户后，已有 access_token 缓存最长 5 分钟失效；期间 API 调用可能仍成功 | 安全边界非实时 | 在五分钟后验证禁用生效；如需立即生效，可重启 API 或等待 token 自然过期 |
| KN-V1.3-02 | 系统健康检测结果有 30 秒缓存，不会实时反映组件状态变化 | 监控时效性 | 可通过间隔 30 秒以上刷新获取最新状态 |
| KN-V1.3-03 | 备份脚本依赖主机安装 `pg_dump`，且在 Docker 宿主机上执行；非 Docker 部署需手工调整 | 备份可用性 | 确保 Docker 宿主机安装 PostgreSQL 客户端工具集 |
| KN-V1.3-04 | 存储状态页仅展示当前 provider 的容量概览，不支持在线切换存储后端或自动迁移已有文件 | 运维灵活性 | 切换 FileSystem / AliyunOss / MinIO 前需制定独立迁移与回滚计划 |
| KN-V1.3-05 | 操作日志筛选结果不支持 CSV 导出，当前仅限页面查看 | 审计导出 | 需导出时可通过 API 分页自行处理 |
| KN-V1.3-06 | 创建用户时无法通过 UI 分配角色（管理员/普通用户）；新建用户默认为普通用户，分配角色需通过 API 或数据库操作 | 管理效率 | 可通过 Swagger 调用 `/api/app/admin/users/{id}/roles` 分配角色 |
| KN-V1.3-07 | Settings 页面信息架构（IA）有调整，管理员用户需要适应新的管理区入口位置 | 体验过渡 | 管理入口集中在 Settings 顶部区域，按角色可见 |
| KN-V1.3-08 | 故障诊断清单为静态内容，不会根据当前系统状态动态展开/收起 | 诊断灵活性 | 静态内容已覆盖常见问题类别 |
| KN-V1.3-09 | 管理端当前仅通过 MAUI Settings 入口和 Swagger/API 提供，无独立 Web 管理后台 | 操作平台 | 独立 Blazor/Web 管理端列为 V2 候选 |
| KN-V1.3-10 | iOS 客户端不在 V1.3 范围内；MAUI 构建仅验证 Windows 和 Android 目标 | 平台覆盖 | 如需 iOS，请参考 V1.2 已知限制并等待后续版本 |
| KN-V1.3-11 | ABP 测试项目（TestBase、EFCore.Tests、ConsoleTestApp）仍使用 10.3.0，未同步升级至 10.5.0 | 测试环境 | 生产 src 项目已全部升级至 10.5.0；Scriban 7.2.5 直接覆盖已消除 4 个 CVE；测试项目不部署到生产环境；详见 [依赖漏洞登记表](dependency-vulnerability-register-v1.3.md) |

## 8. V1.3b 维护版已知限制与技术债务基线

V1.3b 是 V1.3 的移动端验收收口和维护版，不新增后端 API、不新增数据库表、不改变认证、存储、上传下载、媒体处理或分享公开访问边界。以下限制不阻塞 V1.3b 维护版发布，但必须在验收和后续版本规划中保留：

| 编号 | 限制 | 影响 | 规避/备注 |
|:----:|------|------|-----------|
| KN-V1.3b-01 | V1.3b 仅验证和收口 V1.3 已有移动端页面与文档，不引入新的后端能力 | 发布范围 | 新功能进入后续版本规划；V1.3 后端 API 维持冻结 |
| KN-V1.3b-02 | 移动端 UI 验收仍以人工截图和手动路径验证为主，暂无自动化 UI 验收框架 | 验收效率与回归风险 | 每次发布需保留 Settings、分享风险、回收站、故障诊断、存储配置、操作日志等页面截图证据；后续由 mobile-eng 评估 MAUI UI 自动化 |
| KN-V1.3b-03 | `known-limitations.md` 仍依赖发布收口时人工同步 | 文档一致性 | 发布前将 release notes、验收矩阵和本文逐项交叉检查；后续由 docs-writer 设计同步检查清单或脚本 |
| KN-V1.3b-04 | 故障诊断页面为静态排障内容，不会按当前系统状态动态展开或生成诊断结论 | 诊断准确性 | 将其定位为用户自助排障入口；真实系统状态仍以健康页、API 返回和部署日志为准 |
