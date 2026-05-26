# QA 测试账号 Secret 注入与本地栈验证 Runbook（DevOps）

## 目标

为低权限 QA 测试账号提供可重复、可审计、不会泄露凭据的 DevOps 注入流程。该流程与后端 seed 任务通过 `PCD_QA_TEST_ACCOUNT_*` 环境变量契约对齐。

## Secret 存放约定

- 本地真实 secret 只能放在 `.secrets/`，该目录已加入 `.gitignore`。
- 仓库只提交 `.secrets.example`，用于说明文件名和格式，不包含真实密码、access token 或 refresh token。
- 推荐本地文件：`.secrets/qa-test-account.password`。
- CI / 预发环境使用平台 Secret 注入，不提交 `.env` 或 secret 文件。

## 环境变量契约

| 变量 | 说明 | 是否敏感 | 默认值/示例 |
| --- | --- | --- | --- |
| `PCD_QA_TEST_ACCOUNT_ENABLED` | 是否启用 QA 账号 seed | 否 | `false` |
| `PCD_QA_TEST_ACCOUNT_USER_NAME` | 主 QA 用户名 | 否 | `qa_user` |
| `PCD_QA_TEST_ACCOUNT_ALT_USER_NAME` | 隔离验证备用用户名 | 否 | `qa_user_alt` |
| `PCD_QA_TEST_ACCOUNT_ROLE` | 低权限角色 | 否 | `QA.Tester` |
| `PCD_QA_TEST_ACCOUNT_PASSWORD` | QA 账号密码，由 secret manager/CI 注入 | 是 | 不在文档中填写 |
| `PCD_QA_TEST_ACCOUNT_PASSWORD_FILE` | 本地密码文件路径 | 路径非敏感，内容敏感 | `.secrets/qa-test-account.password` |
| `PCD_QA_TEST_ACCOUNT_SECRET_ID` | secret 标识符/引用名 | 否 | `ci:PCD_QA_TEST_ACCOUNT_PASSWORD` |
| `PCD_QA_TEST_ACCOUNT_ROTATED_AT` | 最近轮换时间 | 否 | ISO-8601 日期时间 |

## 本地执行

1. 复制 `.env.example` 为 `.env`，不要把 `.env` 提交。
2. 创建 `.secrets/qa-test-account.password`，写入本地密码。
3. 在 `.env` 设置：
   - `PCD_QA_TEST_ACCOUNT_ENABLED=true`
   - `PCD_QA_TEST_ACCOUNT_PASSWORD_FILE=.secrets/qa-test-account.password`
   - `PCD_QA_TEST_ACCOUNT_SECRET_ID=local/.secrets/qa-test-account.password`
   - `PCD_QA_TEST_ACCOUNT_ROTATED_AT=<轮换时间>`
4. 运行：`bash scripts/prepare-qa-test-account.sh`。

脚本 stdout 只允许输出：用户名、备用用户名、角色、secret 标识符、轮换时间、`sanitized=true` 和迁移状态。不得输出密码、access token、refresh token、Bearer token、Cookie。

## CI / 预发注入方式

- Secret 名称：`PCD_QA_TEST_ACCOUNT_PASSWORD`。
- 非敏感元数据可作为普通变量或 Secret：`PCD_QA_TEST_ACCOUNT_SECRET_ID`、`PCD_QA_TEST_ACCOUNT_ROTATED_AT`。
- CI 作业在运行 DbMigrator 前导出 `PCD_QA_TEST_ACCOUNT_ENABLED=true` 与上述变量。
- CI 不依赖 `PCD_QA_TEST_ACCOUNT_PASSWORD_FILE`，避免把文件路径与 runner 持久化状态绑定。

## 本地栈验证

运行：

```bash
pwsh -NoLogo -NoProfile -File scripts/verify-local-stack.ps1 -PreflightOnly
```

新增 `qa-test-account` 检查只输出：

- `user`
- `alt_user`
- `role`
- `secret_id`
- `rotated_at`
- `sanitized=true`

## 证据模板安全要求

验证证据可记录命令、PASS/WARN/FAIL 数量、用户名、角色、secret 标识符、轮换时间和脱敏结论。证据中禁止出现：

- 明文密码
- access token / refresh token / id token
- `Authorization: Bearer ...`
- Cookie / Set-Cookie
- 可复用私有下载 URL

提交前运行：

```bash
python scripts/secret-log-scan.py --repo-root . --validation-dir docs/validation --include-working-tree
```

## 风险与回滚

风险：

- 后端 seed 尚未实现时，`prepare-qa-test-account.sh` 只能验证 secret 注入与 DbMigrator 调用入口，不能证明账号已创建。
- CI 若误把 `PCD_QA_TEST_ACCOUNT_PASSWORD` 打印到日志，会造成凭据泄露。

回滚：

1. 将 `PCD_QA_TEST_ACCOUNT_ENABLED=false`。
2. 移除 CI/预发环境中的 `PCD_QA_TEST_ACCOUNT_PASSWORD` secret 注入。
3. 删除本地 `.secrets/qa-test-account.password`。
4. 若后端已创建账号，由后端 seed/DBA 任务禁用或删除 `qa_user` 与 `qa_user_alt`，并撤销 `QA.Tester` 绑定。
