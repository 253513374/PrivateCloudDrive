#!/usr/bin/env bash
# =============================================================================
# Git 工作区健康守卫 — PrivateCloudDrive
# 用途: 每个 worker 在开始任何 git 操作前运行此脚本，确保工作区健康
# 退出码: 0=健康  1=损坏需自愈  2=无法自愈需人工
# =============================================================================
set -euo pipefail

MAIN_REPO="D:/Devs/Projects/Personal/PrivateCloudDrive"
TASKS_DIR="D:/Devs/Projects/Personal/PrivateCloudDrive-tasks"
GITHUB_REMOTE="https://github.com/253513374/PrivateCloudDrive.git"
TASK_ID="${HERMES_KANBAN_TASK_ID:-unknown}"

# ── 颜色 ──
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
log()  { echo -e "${GREEN}[OK]${NC}  $*" >&2; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*" >&2; }
err()  { echo -e "${RED}[ERR]${NC} $*" >&2; }

# ── 1. 检查主仓库健康 ──
check_main_repo() {
    if [ ! -d "$MAIN_REPO/.git" ]; then
        err "主仓库 .git 目录不存在"
        return 1
    fi
    if [ ! -f "$MAIN_REPO/.git/HEAD" ]; then
        err "主仓库 .git/HEAD 缺失"
        return 1
    fi
    if [ ! -d "$MAIN_REPO/.git/refs" ]; then
        err "主仓库 .git/refs 缺失"
        return 1
    fi
    # 尝试 git status
    if ! git -C "$MAIN_REPO" status --short &>/dev/null; then
        err "git status 失败，仓库可能损坏"
        return 1
    fi
    return 0
}

# ── 2. 主仓库自愈 ──
heal_main_repo() {
    warn "尝试自愈主仓库..."

    # 方案A: 先尝试移动损坏仓库到备份
    local backup_dir="D:/Devs/Projects/Personal/PrivateCloudDrive.broken-$(date +%Y%m%d-%H%M%S)"
    if [ -d "$MAIN_REPO" ] && mv "$MAIN_REPO" "$backup_dir" 2>/dev/null; then
        # 移动成功，从干净位置克隆
        if git clone -q "$GITHUB_REMOTE" "$MAIN_REPO" 2>/dev/null; then
            log "主仓库自愈成功 (方案A: 移动+重新克隆)"
            return 0
        fi
        # 克隆失败，恢复原目录
        mv "$backup_dir" "$MAIN_REPO" 2>/dev/null
        err "克隆失败，已恢复原仓库"
        return 1
    fi

    # 方案B: 目录被占用，原地修复
    warn "目录被占用，使用原地修复..."
    cd "$MAIN_REPO" || return 1

    # 清理损坏的 .git 和临时目录
    rm -rf .git .git.* _* *-work* clone* repo* ops src* work-* api-contract-work* 2>/dev/null || true

    # 原地重建 Git
    git init -q 2>/dev/null
    git remote add origin "$GITHUB_REMOTE" 2>/dev/null
    if git fetch -q origin main 2>/dev/null && git reset --hard origin/main 2>/dev/null; then
        log "主仓库自愈成功 (方案B: 原地重建)"
        return 0
    fi

    err "主仓库自愈失败 (方案A和方案B均失败)"
    return 1
}

# ── 3. 创建隔离工作区 ──
create_isolated_workspace() {
    mkdir -p "$TASKS_DIR"

    local workspace="$TASKS_DIR/${TASK_ID}"

    if [ -d "$workspace/.git" ]; then
        # 工作区已存在，fetch 更新
        git -C "$workspace" fetch -q origin main 2>/dev/null && \
            git -C "$workspace" reset --hard -q origin/main 2>/dev/null && \
            log "复用已有工作区: $workspace" && \
            echo "$workspace" && return 0
        # 复用失败，清理重建
        rm -rf "$workspace"
    fi

    # 从主仓库本地克隆（快）或从 GitHub 克隆（慢）
    if check_main_repo; then
        git clone -q --shared "$MAIN_REPO" "$workspace" 2>/dev/null && \
            log "从主仓库本地克隆: $workspace" && \
            echo "$workspace" && return 0
    fi

    # 回退到远程克隆
    git clone -q "$GITHUB_REMOTE" "$workspace" 2>/dev/null && \
        log "从 GitHub 远程克隆: $workspace" && \
        echo "$workspace" && return 0

    err "无法创建隔离工作区"
    return 1
}

# ── 4. 清理过期工作区 (超过 24 小时的) ──
cleanup_stale_workspaces() {
    find "$TASKS_DIR" -maxdepth 1 -type d -mmin +1440 -name "t_*" 2>/dev/null | while read -r dir; do
        warn "清理过期工作区: $dir"
        rm -rf "$dir"
    done
    log "过期工作区清理完成"
}

# ── 主流程 ──
main() {
    echo "=== Git 工作区健康守卫 ===" >&2
    echo "任务ID: $TASK_ID" >&2
    echo "主仓库: $MAIN_REPO" >&2

    # Step 1: 检查主仓库
    if check_main_repo; then
        log "主仓库健康"
    else
        warn "主仓库不健康，尝试自愈..."
        if ! heal_main_repo; then
            err "主仓库自愈失败，无法继续"
            exit 2
        fi
    fi

    # Step 2: 创建隔离工作区
    local workspace
    workspace=$(create_isolated_workspace)
    log "隔离工作区就绪: $workspace"

    # Step 3: 清理过期工作区
    cleanup_stale_workspaces

    # Step 4: 输出工作区路径给调用方
    echo "WORKSPACE=$workspace"
    exit 0
}

main "$@"
