# Android 最终可见验收报告（D7 / 2026-05-26）

结论：PASS（发布建议：有条件通过）

本报告替代 `docs/validation/android-backup-evidence-t_3399b1c7/README.md` 中“登录后备份中心、队列重试、容量/健康页截图未完成”的历史阻塞结论。当前 main 已沉淀 slice1～slice5 Android 截图、Android 登录错误分类记录、后端恢复/回收站烟测记录和 validation evidence index；本报告将这些证据收口为单一可读 PASS/WARN/FAIL 报告。

有条件通过口径：Android 模拟器可见主链路证据已覆盖登录后文件/相册/备份队列/容量健康/失败重试等 D7 阻塞项，未发现敏感信息泄露或阻断缺陷；删除/恢复在后端 DR 烟测已 PASS、Android 代码具备回收站入口与文案，但本次仓库内没有“Android 删除→回收站→还原”的独立可见截图，因此列为非阻断 WARN，建议下一轮补截图后升级为完全通过。

## 1. 验收环境与证据来源

| 项 | 说明 |
| --- | --- |
| 仓库基线 | main @ `39655e8 修复公开分享 Swagger 路由冲突 (#25)` |
| 工作区 | `D:/Devs/Projects/Personal/PrivateCloudDrive-tasks/t_android_final_evidence/repo` |
| Android 设备 | Pixel 9 Pro API 36 模拟器截图，分辨率均为 1280x2856 |
| App 包口径 | MAUI Android Debug APK，`EmbedAssembliesIntoApk=true`，`AndroidFastDeploymentType=None`（历史 t_3399b1c7 构建记录为 0 errors） |
| 可见证据 | `docs/validation/screenshots/private-backup-slice1~slice5-*.png`、`docs/validation/android-backup-evidence-t_3399b1c7/*` |
| 文本/后端证据 | `docs/validation/pcd-real-backup-slice4.txt`、`docs/validation/pcd-retry-slice5.txt`、`docs/validation/backup-restore-destructive-test-stack-20260521-215020.md`、`docs/validation/android-login-error-classification-t_6b53cfe3-20260522.md` |
| 脱敏口径 | 截图只包含测试文件名、容量摘要、状态文案；未出现 Token、Cookie、密码、连接串、完整公开分享 URL 或真实私密文件内容 |

## 2. PASS/WARN/FAIL 汇总

| 状态 | 数量 | 项目 |
| --- | ---: | --- |
| PASS | 10 | 登录/启动、文件列表、照片备份入口、视频/文件备份入口、批量备份队列、上传完成、失败保留与重试成功、下载/本地文件列表、容量/健康与恢复边界说明、隐私/脱敏边界 |
| WARN | 1 | Android 删除/恢复缺少独立截图；后端 DR 烟测已覆盖 trash restore，App 代码具备 Trash/Restore/Delete 文案和入口，不作为 D7 阻断 |
| FAIL | 0 | 未发现阻断缺陷 |

## 3. 功能验收明细

| 编号 | 验收项 | 证据 | 结果 | QA 结论 |
| --- | --- | --- | --- | --- |
| A1 | 干净启动与登录入口 | `android-backup-evidence-t_3399b1c7/01-clean-launch.png`、`01-clean-launch-window.xml`、`logcat-clean-launch.txt`；`screenshots/private-backup-slice1-login-server-config.png` | PASS | 清理数据后可进入登录页，默认后端配置与账号登录入口可见；logcat 摘要无 App 侧 `FATAL EXCEPTION`。 |
| A2 | 登录后文件中心 | `screenshots/private-backup-slice2-files-after-login.png` | PASS | 登录后可见文件页/根目录内容；支持后续备份与下载验证路径。 |
| A3 | 照片备份 | `screenshots/private-backup-slice2-backup-queue.png`、`screenshots/private-backup-slice4-start-backup-page.png` | PASS | 备份页可见照片/文件备份入口与队列状态。 |
| A4 | 视频/本机文件备份 | `screenshots/private-backup-slice4-start-backup-page.png`、`pcd-real-backup-slice4.txt` | PASS | 使用小型无隐私测试文件作为上传样本；不记录真实私密内容。 |
| A5 | 批量备份队列与上传完成 | `screenshots/private-backup-slice4-upload-result.png` | PASS | 备份队列显示 `pcd-real-backup-slice4.txt` 已完成，进度 100%。 |
| A6 | 失败保留与重试成功 | `screenshots/private-backup-slice5-server-down-failed-queue.png`、`screenshots/private-backup-slice5-retry-success.png`、`pcd-retry-slice5.txt` | PASS | 后端不可用时任务保留为失败并提供“重试备份”；服务恢复后同一任务 100% 已完成。 |
| A7 | 下载/预览入口与本地下载列表 | `screenshots/private-backup-slice4-downloads-list.png` | PASS | Android 下载列表中可见测试文件 `pcd-real-backup-s...` 和分类筛选；该截图证明下载/本地文件可见路径，具体内容预览已由后端 DR smoke 的 Range download/content hash 覆盖。 |
| A8 | 删除/恢复 | `backup-restore-destructive-test-stack-20260521-215020.md`；App 源码 `AppText.Trash/Restore/DeleteForever` | WARN | 后端 disposable stack 已验证 delete to trash、trash list、restore PASS；当前证据包未包含 Android 删除→回收站→还原截图。此为证据完整性 WARN，不是已知功能阻断。 |
| A9 | 容量/健康 | `screenshots/private-backup-slice3-storage-usage.png`、`screenshots/private-backup-slice3-settings-health.png` | PASS | 存储用量、后端存储类型、磁盘剩余、配额、API 可访问与健康状态可见；未暴露连接串或绝对存储路径。 |
| A10 | 恢复说明/隐私边界 | `screenshots/private-backup-slice3-storage-usage.png`、`android-login-error-classification-t_6b53cfe3-20260522.md` | PASS | 页面展示“备份建议/了解智能整理”等用户级说明；登录错误分类固定文案，不展示 host、端口、URL、Socket 原文、OAuth error_description 或异常堆栈。 |

## 4. 证据包清单

### 4.1 Android 截图

| 文件 | 用途 | 尺寸 | 状态 |
| --- | --- | --- | --- |
| `docs/validation/android-backup-evidence-t_3399b1c7/01-clean-launch.png` | 干净启动/登录页 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice1-startup.png` | App 启动 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice1-login-server-config.png` | 登录/服务器配置 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice2-files-after-login.png` | 登录后文件中心 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice2-backup-queue.png` | 初始备份队列 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice2-create-action.png` | 创建/操作入口 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice3-storage-usage.png` | 容量/健康/恢复建议 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice3-settings-health.png` | 设置/健康入口 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice4-start-backup-page.png` | 备份发起页 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice4-upload-result.png` | 上传完成 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice4-downloads-list.png` | 下载列表/本地文件可见 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice4-duplicate-failure-retry.png` | 重复/失败处理 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice4-auth-expired-patched-result.png` | 授权过期错误文案 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice5-before-failure.png` | 失败前队列状态 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice5-login-before-failure.png` | 失败场景登录前置状态 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice5-picker-after-api-stop.png` | 后端停止后选择文件 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice5-server-down-failed-queue.png` | 服务不可用失败队列 | 1280x2856 | PASS |
| `docs/validation/screenshots/private-backup-slice5-retry-success.png` | 重试成功 | 1280x2856 | PASS |

### 4.2 文本与后端证据

| 文件 | 用途 | 状态 |
| --- | --- | --- |
| `docs/validation/android-backup-evidence-t_3399b1c7/logcat-clean-launch.txt` | Android clean launch logcat 摘要 | PASS |
| `docs/validation/android-backup-evidence-t_3399b1c7/01-clean-launch-window.xml` | 登录页 UIAutomator 层级 | PASS |
| `docs/validation/pcd-real-backup-slice4.txt` | 真实备份小样本，内容无隐私 | PASS |
| `docs/validation/pcd-retry-slice5.txt` | 失败重试小样本，内容无隐私 | PASS |
| `docs/validation/backup-restore-destructive-test-stack-20260521-215020.md` | disposable stack 恢复、下载/预览、trash restore 与审计 smoke | PASS |
| `docs/validation/android-login-error-classification-t_6b53cfe3-20260522.md` | 登录错误安全分类与 raw exception 防泄漏 | PASS |

## 5. 脱敏/敏感信息复核

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 截图人工复核 | PASS | 截图只展示测试文件名、容量摘要、状态文案和通用文件分类；未见密码、Token、Cookie、完整私有/公开分享 URL、连接串或真实个人内容。 |
| 文本证据扫描 | PASS | 使用 `scripts/validation_evidence_index.py` 生成本次索引，要求 finding_count=0。 |
| 大日志控制 | PASS | 提交范围只保留裁剪后的 logcat 摘要和 Markdown/TXT 小证据；未提交原始大日志。 |

## 6. validation_evidence_index

执行命令：

```bash
python scripts/validation_evidence_index.py --run-id android-final-visible-acceptance-20260526 --date 20260526
```

实际结果：`status=PASS`、`evidence_count=17`、`finding_count=0`。生成目录：`docs/validation/daily-acceptance-20260526-android-final-visible-acceptance-20260526/`（本地/CI artifact 用途，不作为人工报告主体）。

## 7. 发布建议

| 维度 | 结论 | 说明 |
| --- | --- | --- |
| Android 最终可见证据包 | PASS | 已形成单一可读报告，并将历史“登录后截图未完成”阻塞改写为已被 slice1～slice5 证据覆盖。 |
| D7 发布闸门中的 Android 证据项 | 有条件通过 | 不再作为阻断项；删除/恢复 Android 独立截图作为 WARN 跟踪。 |
| 是否需要创建阻断修复任务 | 否 | 未发现 Android/后端阻断缺陷。 |
| 建议发布结论 | 有条件通过 | 可进入下一轮 release-manager 综合闸门；若最终用户验收要求“每个子能力均有 Android 独立截图”，需补拍删除/恢复截图后升级为完全通过。 |

## 8. 后续非阻断建议

1. 下一次 Android 可见验收补充 3 张截图：文件详情删除确认、回收站列表、还原成功后文件回到列表。
2. 若用户后续要求真实设备覆盖，在当前模拟器 PASS 基础上增加真机 smoke；当前阶段用户已接受 Android 模拟器作为替代，不作为 D7 阻断。
3. 保持 `docs/validation/README.md` 的证据提交策略：完整原始日志留在 ignored artifact 或 CI artifact，Git 只提交可公开摘要。
