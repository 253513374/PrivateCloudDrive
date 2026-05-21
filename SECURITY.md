# Security Policy

PrivateCloudDrive 处理用户文件、分享链接、登录 Token、数据库和存储目录，安全问题优先级高于普通功能需求。

## 支持版本

当前公开仓库处于 Private Backup MVP / Productization 阶段。默认只支持 `main` 分支最新代码的安全修复。

## 报告安全问题

请不要在公开 Issue 中披露可利用细节。建议通过 GitHub Security Advisory 私下报告；如果仓库暂未启用 Advisory，请先创建不含利用细节的 Issue，标题使用：

```text
Security: request private disclosure channel
```

维护者会提供后续私下沟通方式。

## 高优先级安全范围

- 越权访问其他用户文件、目录、分享、标签、相册或操作日志。
- 分享链接 Token 可预测、密码验证绕过、下载权限绕过。
- OpenIddict Token、Refresh Token、外部登录绑定票据泄漏或错误复用。
- `.env`、AppSecret、数据库密码、对象存储 AccessKey 被提交、日志打印或返回给客户端。
- 删除、恢复、备份、迁移流程导致用户数据不可逆丢失。
- Docker/部署默认配置在公网下暴露 Swagger、数据库、Redis、MinIO 或调试端口。

## 密钥与配置原则

- 生产 `.env` 禁止提交。
- WeChat / Google / GitHub AppSecret 只能存在于后端环境变量或密钥系统。
- MAUI App 只允许持有公开 ClientId、RedirectUri、Scope 等非敏感配置。
- 本地模板密码只用于开发，生产必须替换。
- 备份目录中如包含 `.env.secret`，必须放入加密和访问受控的存储，禁止提交到 Git。

## 维护响应目标

- 24-72 小时内确认报告。
- 7 天内给出初步影响判断或缓解方案。
- 高危问题优先发布修复和升级说明。
