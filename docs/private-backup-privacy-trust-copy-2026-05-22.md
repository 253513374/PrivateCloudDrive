# Private Backup 隐私信任文案

日期：2026-05-22
适用范围：README、部署文档、灾备文档、App 安全摘要、发布说明和验收证据。

## 1. 对外推荐文案

PrivateCloudDrive 是自托管私有网盘。你的文件、数据库和环境密钥由部署者控制。默认本地部署会把文件数据保存在服务器的 FileCenter storage volume，把账号、权限、文件索引、分享和审计日志保存在 PostgreSQL。

PrivateCloudDrive MVP 当前不承诺端到端加密或零知识加密。部署管理员、数据库/存储/备份介质访问者可能接触原始文件或元数据。请保护服务器、`.env`、数据库备份、storage 归档、对象存储密钥和备份介质。

## 2. 数据和密钥边界

| 类别 | 存放位置 | 谁需要保护 | 公开证据允许记录 |
| --- | --- | --- | --- |
| 文件正文/缩略图/视频封面 | FileCenter storage volume 或对象存储 | 部署者/管理员 | 脱敏文件名、大小、类型、哈希摘要 |
| 账号/权限/索引/审计 | PostgreSQL | 部署者/管理员 | 状态码、PASS/WARN/FAIL、脱敏样本 |
| `.env` / 加密短语 | 服务器本地或密钥系统 | 部署者/管理员 | 只允许写“需配置/已替换”，不得记录原文 |
| OAuth / 微信 AppSecret | 后端配置或密钥系统 | 部署者/管理员 | 不允许记录，App 端不得内置 |
| access token / refresh token / cookie | 客户端安全存储、请求头、服务端校验 | 系统和用户设备 | 不允许记录 |
| 分享链接 | 用户主动创建 | 用户/管理员 | 只允许脱敏 URL 或 token 后四位 |

## 3. 不得承诺

- 不得承诺 E2EE、零知识加密、管理员不可见原始文件。
- 不得承诺手机缓存可以恢复服务器文件。
- 不得承诺默认 `storage.tar.gz` 覆盖 Aliyun OSS bucket/object。
- 不得承诺 MinIO 已是当前默认 FileCenter Provider。
- 不得承诺公开分享链接不会扩大访问边界；应说明分享链接和密码保护的责任。

## 4. App 可见文案红线

App 和截图中不得出现：

- 服务器绝对路径、真实 bucket 名、对象 key、AccessKey、连接串。
- `.env` 原文、数据库密码、OAuth/微信 AppSecret。
- access token、refresh token、cookie、OAuth code。
- raw exception、堆栈、内部配置键。
- 用户真实隐私文件内容。

## 5. 发布验收清单

| 检查 | 通过标准 |
| --- | --- |
| README | 有隐私、数据位置和恢复责任入口；不夸大加密能力 |
| deployment | 说明生产必须替换模板密钥、保护 `.env`，对象存储密钥仅后端持有 |
| disaster-recovery | 说明默认备份覆盖 PostgreSQL + FileCenter storage，不覆盖 Aliyun OSS bucket |
| known limitations | 列出 E2EE/零知识、MinIO、OSS 备份、真机/iOS 回归等限制 |
| App 截图 | 不泄漏 token/cookie/private URL/绝对路径/bucket/AccessKey |
| validation 报告 | 只记录脱敏证据和 PASS/WARN/FAIL |

## 6. 下游交接

- DevOps：保护 `.env` 和备份介质；生产部署前替换所有模板密钥。
- 后端：系统健康与错误摘要只返回安全、可行动信息。
- 移动端：Settings、Uploads、Login 等页面不展示 raw exception 或内部地址细节。
- QA：截图、logcat、报告进入仓库前必须做敏感信息扫描。
- Release Manager：任一红线命中时，Public RC 不得通过。
