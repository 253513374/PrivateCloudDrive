# PrivateCloudDrive Git 治理规范

更新时间：2026-05-22

## 1. 目标

本规范用于保证 PrivateCloudDrive 多 Agent 并行开发时：

1. 共享主库 `main` 始终保持可用、可验证、可继续派生。
2. 所有实现任务都在隔离工作区中完成，不直接污染共享主库。
3. 每个阶段完成后，优先把已通过门禁的阶段成果合并回 `main`，再从最新 `main` 派生下一阶段任务和分支。

## 2. 非协商规则

1. 禁止 worker 直接在共享主库 `D:/Devs/Projects/Personal/PrivateCloudDrive` 中开发。
2. 所有实现任务必须在隔离工作区 `D:/Devs/Projects/Personal/PrivateCloudDrive-tasks/t_<task_id>/` 中执行。
3. 所有变更必须通过任务分支 + Pull Request 合入 `main`。
4. `main` 受 GitHub Branch Protection 保护，必须通过：
   - `Public repo quality gate`
   - 会话已解决（required conversation resolution）
   - 禁止 force push / delete
   - 线性历史
   - 当前单人维护模式下不要求非作者审批；如未来切回多人协作，再恢复 approval 门禁
5. 如发现共享主库损坏、只剩残缺 `.git`、或被 IDE/后台进程占用，必须立刻止血、恢复主库，再继续调度。

## 3. 标准开发流

### 3.1 任务启动前

每个 worker 在任何 git 操作前必须先运行：

```bash
set -euo pipefail
bash D:/Devs/Projects/Personal/PrivateCloudDrive/scripts/git-workspace-guard.sh > /tmp/pcd-workspace-guard.out
WORKSPACE=$(grep '^WORKSPACE=' /tmp/pcd-workspace-guard.out | cut -d= -f2)
test -n "$WORKSPACE"
cd "$WORKSPACE"
```

作用：

1. 健康检查共享主库
2. 必要时自动自愈
3. 创建任务隔离工作区
4. 返回当前任务可用目录

### 3.2 分支规范

每个任务必须从最新 `main` 派生任务分支：

```text
agent/<task-id>/<scope>
```

例如：

```text
agent/t_8849deee/android-login-error-classification
```

### 3.3 阶段推进规范（新增）

这是当前项目的强制节奏：

1. 一个阶段形成可验收成果后，不允许长期堆积在多个未合并分支上。
2. 对于已经通过代码检查、CI、QA/验收门禁的阶段成果，应优先推进 review/merge，尽快合并回 `main`。
3. 只有在该阶段成果回到 `main` 后，才批量派发下一阶段的实现任务。
4. 后续任务创建分支前，必须基于最新 `main` 重新派生，避免从陈旧分支继续分叉开发。
5. 如多个 PR 同时存在，优先合并“后续任务的共同基线 PR”，再派发其下游任务。

一句话原则：

> 每完成一个阶段，先把阶段成果合回主库，再从最新主库继续下一阶段。

## 4. 阶段收口检查表

当 Hermes 判断某个阶段准备进入下一阶段时，必须先确认：

1. 该阶段关键 PR 已创建并进入门禁。
2. 可合并 PR 已完成：
   - `git diff --check`
   - 必要测试 / 构建
   - CI 通过
   - 无敏感信息泄漏
   - 已满足 review 条件
3. 若 PR 仅剩非作者审批，则将其标记为“治理门禁阻塞”，不与技术阻塞混淆。
4. 在派发下一阶段任务前，先同步并确认共享主库是最新健康 `main`。

## 5. 看板调度规则

1. 不要盲目 unblock 所有 blocked 任务。
2. blocked 任务需先区分：
   - 真正技术阻塞
   - 上游依赖未完成
   - 仅剩 PR 审批门禁
   - profile / skill / provider 崩溃问题
3. 对已经形成 PR 且通过门禁的历史 blocked 卡，应关闭旧卡，避免重复派发。
4. 下一轮重派应优先选择：
   - 以最新 `main` 为基线的任务
   - 不依赖陈旧分支的任务
   - 不再强绑已知会崩溃 skill 参数的任务

## 6. 主库保护与恢复

如共享主库异常：

1. 立即阻断新任务，防止继续写坏主库。
2. 终止占用主库的 IDE / DevHub / Copilot / 其他驻留进程。
3. 将损坏目录备份为 `PrivateCloudDrive.broken-restore-<timestamp>`。
4. 从 GitHub fresh clone 恢复主库。
5. 运行以下检查：
   - `git fsck`
   - `git status`
   - `scripts/git-workspace-guard.sh`
   - `scripts/board-watchdog.sh`
   - 如需检查定时巡检链路，再额外确认 Hermes scheduler wrapper：`C:/Users/q4528/AppData/Local/hermes/scripts/pcd-watchdog.sh`
6. 只有主库恢复健康后，才允许继续 dispatch。

## 7. 执行口径

Hermes 后续在 PrivateCloudDrive 的默认执行口径为：

- 先保主库健康
- 再保阶段成果及时回主库
- 再从最新主库派发下一阶段任务

如阶段成果尚未合回 `main`，原则上不应大规模展开下一阶段开发，除非存在明确的并行必要性且不会污染后续分支基线。
