# D7 发布闸门复核：Android 备份证据包

复核人：芮发布（发布经理 / release-manager）

复核对象：PR #26 `docs/android-evidence-pack` — https://github.com/253513374/PrivateCloudDrive/pull/26

复核结论：D7 Android 备份闭环“证据包闸门”通过；总发布建议为 CONDITIONAL GO / HOLD，不升级用户最终验收，直到 PR #26 合入 main 且 GitHub Actions 外部/账号基础设施故障解除或由项目负责人明确接受豁免。

## 1. 闸门裁决

| 闸门项 | 结论 | 证据 | 发布影响 |
|---|---|---|---|
| Android 备份证据包完整性 | PASS | `docs/validation/android-backup-release-evidence.md` 第 1、2、5、7 节 | 可作为 D7 发布说明和验收摘要输入。 |
| Android 模拟器闭环 | PASS | 备份入口、文件备份、队列状态、失败重试、下载列表、容量/健康截图索引见 `android-backup-release-evidence.md` 第 2 节 | 当前阶段可按“模拟器验收通过”表述。 |
| 后端恢复/下载/预览/删除恢复补强 | PASS | `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`，由证据包第 2.6 节引用 | 支撑 Android 尚未完整覆盖的删除/恢复深链路。 |
| D7 daily acceptance / 敏感扫描 | PASS | `daily-acceptance-20260523-review-7/23/24/validation-evidence-index.md` 均显示 `Status: PASS` 与 `Sensitive findings: 0` | 公开证据文本可引用；未发现 token/cookie/password/完整私有 URL。 |
| 真机与弱网覆盖 | WARN | `android-backup-release-evidence.md` 第 5 节 | 不阻断当前 D7 证据闸门，但发布说明必须列为待增强，不得宣传为已真机覆盖。 |
| PR #26 合入状态 | HOLD | PR #26 仍为 OPEN，未合入 main | 在合入前，main 分支仍缺失该证据包，不能把 main 视作最终发布基线。 |
| GitHub Actions 状态 | HOLD / 外部基础设施异常 | `Public repo quality gate` 下载 `actions/setup-dotnet@v5` 失败；`Validation evidence sensitive-data gate` checkout 返回 GitHub 403 `Your account is suspended`；`GitGuardian Security Checks` PASS | 这两项失败没有进入项目构建/扫描逻辑，不能证明内容失败；但作为受保护分支门禁仍阻止自动合入/最终发布。 |

## 2. 可直接引用的发布摘要

Android 私有备份 MVP 已在模拟器完成备份入口、文件备份、队列状态、失败重试、容量/健康和恢复说明可见性验收；后端隔离恢复烟测补强下载/预览、删除/恢复和审计边界。D7 evidence index 显示敏感发现为 0。真机相册/视频大批量、后台续传、弱网和 OEM 差异仍列为后续增强验证。

## 3. 本次复核执行的本地检查

| 检查 | 结果 |
|---|---|
| PR #26 变更文件清单复核 | PASS，变更集中在 `docs/validation/README.md`、`android-backup-release-evidence.md` 和 D7 daily acceptance 索引/扫描摘要。 |
| 关键证据路径存在 | PASS：Android 构建日志、logcat 裁剪摘要、备份入口截图、重试成功截图均存在。 |
| D7 sensitive findings 摘要 | PASS：review-7、review-23、review-24 均为 `Sensitive findings: 0`。 |
| Markdown diff 检查 | PASS：`git diff --check origin/main...HEAD -- docs/validation/android-backup-release-evidence.md docs/validation/README.md` 无错误。 |
| 关键词敏感复核 | PASS with manual false positives：命中均为规则说明或“未发现”文字，没有真实 token/cookie/password/client_secret/Bearer 值。 |

## 4. 发布说明必须保留的限制

1. 当前证据是 Android 模拟器可见验收，不等同于真机相册权限、真实媒体库大批量照片/视频、后台续传、系统杀进程恢复、弱网/OEM 差异已完成。
2. “扫描文档（即将推出）”只可作为 UI 中的未推出入口说明，不得列入已交付能力。
3. 真实手机访问局域网后端、HTTPS 证书、局域网 DNS/防火墙策略仍需部署环境复核。
4. Android 端删除到回收站并恢复的完整 UI 链路仍建议补真机截图；当前由后端灾备烟测和 Android 入口可见共同支撑。

## 5. 合入与发布前置条件

- PR #26 合入 main，确保最终发布基线包含 Android 备份证据包。
- GitHub Actions 外部/账号基础设施故障解除，或由项目负责人明确接受基于本地复核 + GitGuardian PASS 的一次性门禁豁免。
- 发布说明引用第 2 节摘要，并同步列出第 4 节限制，避免过度宣传真机/弱网/大视频能力。

## 6. 明确建议

芮发布建议：

- 对“Android 备份闭环证据是否足以补齐 D7 证据缺口”：GO。
- 对“PR #26 是否具备内容合入价值”：GO，等待非作者复核/CI 基础设施恢复后合入。
- 对“是否现在升级用户最终人工验收或公开发布”：NO-GO / HOLD。原因不是 Android 证据不足，而是 PR 尚未合入 main，且 GitHub Actions 仍受外部/账号基础设施故障影响。
