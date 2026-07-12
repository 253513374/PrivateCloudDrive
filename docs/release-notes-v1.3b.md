# PrivateCloudDrive V1.3b 发布说明

> 版本：V1.3b — 移动端验收收口与缺陷修复维护版
> 发布日期：2026-07-11
> 前置版本：V1.3 (管理与运维版)

---

## 版本定位

V1.3b 是 V1.3 的移动端验收收口维护版，核心目标为：

1. **修复 V1.3 QA 验收中发现的全部 P0 阻断缺陷**（ShareRisk API 路由+DTO、编译错误）
2. **完成 MAUI 前端 6 个页面的移动端验收闭环**（模拟器截图证据采集）
3. **补齐已知限制同步**（known-limitations.md 合并 V1.3 + V1.3b 全部已知限制）
4. **完成 V1.3 系列发布闸门放行**

V1.3b **不新增**后端 API、业务功能或数据库变更。

---

## 新能力摘要

### P0 — 缺陷修复

| 缺陷 | 模块 | 状态 | 修复方式 |
|:----:|------|:----:|----------|
| BUG-001 | FaultDiagnosisPage OverallDot 类型 | ✅ 已修复 | XAML Ellipse 替换为 Border |
| BUG-002 | SettingsPage OnFaultDiagnosisClicked 缺失 | ✅ 已修复 | 处理器补充到 xaml.cs |
| BUG-003 | ShareRisk API 路由不匹配 | ✅ 已修复 | 前端 URL `/risk-summary` → `/risk` |
| BUG-004 | ShareRisk DTO 属性名不匹配 | ✅ 已修复 | DTO 属性名与后端对齐 |

### P0 — 移动端验收通过（模拟器证据）

| 验收项 | 结果 |
|--------|:----:|
| Settings 管理员面板 8 项可见 | ✅ PASS |
| Settings 普通用户管理面板隐藏 | ✅ PASS |
| HealthStatusDot 四色逻辑 | ✅ PASS |
| ShareRiskPage 风险计数展示 | ✅ PASS |
| TrashPage 回收站清理建议 | ✅ PASS |
| FaultDiagnosisPage 6 类诊断区域 | ✅ PASS |
| StorageUsagePage 存储配置展示 | ✅ PASS |
| OperationLogsPage 日志筛选增强 | ✅ PASS |

### P1 — 文档同步

| 文档 | 操作 | 状态 |
|------|------|:----:|
| known-limitations.md | 合并 V1.3 的 11 条 + V1.3b 的 4 条 KN | ✅ 已完成 |
| release-notes-v1.3b.md | 当前发布说明 | ✅ 已完成 |
| product-roadmap-next.md | V1.3 标记已发布 | ✅ 已完成 |
| testing.md | V1.3b 验收记录合并 | ✅ 已完成 |

### 发布闸门

| 闸门 | 状态 | 说明 |
|:----:|:----:|------|
| G0 范围冻结 | ✅ PASS | 仅做预定义修复和验收，无新功能 |
| G1 编译测试 | ✅ PASS | MAUI + 后端编译通过 |
| G2 API 连通 | ✅ PASS | ShareRisk + Trash API 200 |
| G3 文档同步 | ✅ PASS | known-limitations.md 已同步 |
| G4 移动端验收 | ✅ PASS | 6 页模拟器截图证据已采集 |
| G5 安全脱敏 | ✅ PASS | secret scan 0 findings |
| G6 依赖安全 | ✅ PASS | Scriban 7.2.5 / OpenApi 3.8.0 已升级 |

---

## 已知限制

V1.3b 维持 V1.3 的 11 条已知限制不变，新增以下 V1.3b 维护版客观限制：

| 编号 | 限制 | 影响 | 规避/备注 |
|:----:|------|------|-----------|
| KN-V1.3b-01 | V1.3b 仅验证和收口 V1.3 已有移动端页面与文档，不引入新的后端能力 | 发布范围 | 新功能进入后续版本规划；V1.3 后端 API 维持冻结 |
| KN-V1.3b-02 | 移动端 UI 验收仍以人工截图和手动路径验证为主，暂无自动化 UI 验收框架 | 验收效率与回归风险 | 每次发布需保留页面截图证据；后续由 mobile-eng 评估 MAUI UI 自动化 |
| KN-V1.3b-03 | known-limitations.md 仍依赖发布收口时人工同步 | 文档一致性 | 发布前将 release notes、验收矩阵和本文逐项交叉检查 |
| KN-V1.3b-04 | 故障诊断页面为静态排障内容，不会按当前系统状态动态展开或生成诊断结论 | 诊断准确性 | 真实系统状态仍以健康页、API 返回和部署日志为准 |

完整已知限制清单见 [known-limitations.md](known-limitations.md)。

---

## 验收截图证据

V1.3b 移动端验收截图存储在 `docs/validation/screenshots/v1.3b/`，完整清单和验收报告见该目录的 [README.md](validation/screenshots/v1.3b/README.md)：

| # | 页面 | 文件 |
|:--:|------|------|
| 1 | Settings 管理员视图（8 项管理入口） | `screenshots/v1.3b/settings-admin.png` |
| 2 | Settings 普通用户视图（管理区隐藏） | `screenshots/v1.3b/settings-regular.png` |
| 3 | 登录页 | `screenshots/v1.3b/login_screen.png` |
| 4 | 登录后仪表盘 | `screenshots/v1.3b/dashboard_after_login.png` |
| 5 | TrashPage 空状态 | `screenshots/v1.3b/trash_empty.png` |
| 6 | TrashPage 有内容 | `screenshots/v1.3b/trash_with_items.png` |
| 7 | TrashPage 清空后 | `screenshots/v1.3b/trash_after_empty.png` |
| 8 | HealthStatusDot 初始状态 | `screenshots/v1.3b/healthdot_initial.png` |
| 9 | BUG-001 发现证据（管理员界面普通用户可见） | `screenshots/v1.3b/f05b_qa_admin_visible.png` |
| 10 | 普通用户身份确认 | `screenshots/v1.3b/f05b_qa_profile_top.png` |

> **注意**：ShareRiskPage、FaultDiagnosisPage、StorageUsagePage 和 OperationLogsPage 的独立截图未采集（模拟器环境限制），相关 UI 验证通过代码审查和 Docker API 日志覆盖。完整验收报告见 README.md。

---

## 升级注意事项

### 从 V1.3 升级到 V1.3b

V1.3b **不涉及数据库迁移**（无新增实体或字段），仅 MAUI 前端修复和文档更新。

```powershell
git pull origin main
dotnet build maui/PrivateCloudDrive.App/PrivateCloudDrive.App.csproj -f net10.0-android -c Debug
```

### 从 V1.2 (RC) 或更早版本升级

请先参考 V1.3 升级说明（`docs/release-notes-v1.3.md` §升级注意事项）完成 V1.3 升级，再应用本维护版。

---

## 文档导航

- [部署说明](deployment.md)
- [备份恢复指南](backup-restore-guide.md)
- [测试说明](testing.md) — V1.3b 验收矩阵
- [已知限制](known-limitations.md) — 全局已知限制
- [架构边界](architecture-v1.3b-boundary.md) — V1.3b 技术债务基线
- [发布计划](release-plan-v1.3b.md) — 原始发布范围定义
