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
