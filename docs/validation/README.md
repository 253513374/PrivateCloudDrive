# Validation evidence policy

本目录存放 PrivateCloudDrive 发布验收、移动端可见性验证、备份恢复演练、后端测试与 Kanban 复核证据。发布提交只保留可复核、可公开、体积可控的摘要证据；完整原始输出保留为本地 ignored 文件或 CI artifact。

## 可进入 Git 的证据

| 类型 | 提交口径 |
| --- | --- |
| Markdown 报告/索引 | 可提交；必须说明验证来源、范围、结论、剩余风险，并避免写入敏感原文。 |
| `.log` / logcat | 仅可提交裁剪后的公开摘要；不得提交原始设备日志、系统噪声或大体积流式日志。 |
| `.txt` 小摘要 | 可提交 PASS/WARN/FAIL、命令范围、复核结论；不得包含账户凭据、令牌、Cookie、完整私有链接。 |
| scan 摘要 | 可提交扫描范围、规则类别、命中数量与处置结论；完整扫描导出放 CI artifact。 |
| 截图 | 仅在支撑 UI/移动端验收时提交；需确认无私密信息、完整分享链接或个人敏感数据。 |

## 不进入 Git 的证据

| 类型 | 处理方式 |
| --- | --- |
| 原始 logcat、模拟器 dump、服务端全量日志 | 保留为 ignored local artifact 或 CI artifact；提交前裁剪为公开摘要。 |
| 大体积 Kanban/team 快照，例如 `team-capability-kanban-snapshot-*.json` | 默认不纳入发布提交；如确需复核，先裁剪成小型 Markdown/JSON 摘要。 |
| 完整扫描导出 | 放 CI artifact；Git 中只保留脱敏摘要。 |
| 凭据、认证令牌、Cookie、账户口令、OAuth 客户端密钥、完整私有/分享 URL | 立即移除；如已暴露，需先撤销或轮换，再允许 release gate 继续。 |

## Release gate 结论规则

1. 凭据、认证令牌、Cookie、账户口令、OAuth 客户端密钥、完整私有/分享 URL 任一命中，release gate = FAIL，直到移除并完成必要撤销/轮换。
2. 仅存在已脱敏摘要，且完整证据在 ignored local artifact 或 CI artifact 中可追溯时，可按底层验证结果判定 PASS/WARN。
3. `.log`/logcat 文件若仍是原始大日志，release gate = FAIL；裁剪为小体积公开摘要后可复核。
4. 大体积快照若未裁剪即进入暂存或提交，release gate = FAIL；仅作为 ignored/CI artifact 时不阻断。
5. 每次发布前至少执行 `git diff --check`，并对本目录文本证据执行敏感信息扫描；扫描脚本可使用 `scripts/validation_evidence_index.py` 生成 manifest 与摘要。

## Validation evidence index / sensitive-data gate

`scripts/validation_evidence_index.py` 用于生成每日验收证据索引和敏感信息扫描摘要。

推荐命令：

```bash
python scripts/validation_evidence_index.py --run-id local --date $(date -u +%Y%m%d)
```

退出码契约：

| 退出码 | 含义 |
| ---: | --- |
| 0 | 已生成 `validation-evidence-index.*` 与 `sensitive-scan.*`，且未发现敏感信息。 |
| 2 | 已生成报告，但发现 token/cookie/password/client_secret/完整分享链接等疑似敏感信息；CI/release gate 应阻断。 |
| 1 | 脚本运行错误或环境错误。 |

扫描范围与排除策略：

- 默认扫描 `docs/validation` 下 `.md/.txt/.json/.log/.yml/.yaml/.csv/.xml` 文本证据。
- 自动排除历史 `daily-acceptance-*` 生成目录，避免 `validation-evidence-index.md` / `sensitive-scan.md` 被二次扫描并传播误报。
- `PASS: 14`、`WARN: 0`、`FAIL: 0` 等状态行只作为结果统计，不应被识别为 password 泄漏。

CI 接入：当前由 `.github/workflows/ci.yml` 中的 `Validation evidence sensitive-data gate` 步骤运行该脚本；无敏感命中时通过，有命中时以退出码 2 阻断。无论通过或失败，工作流都会上传 `docs/validation/daily-acceptance-*-${{ github.run_id }}/` 作为 artifact，保留 14 天。
