#!/usr/bin/env bash
# =============================================================================
# PrivateCloudDrive 看板健康看门狗
# 用途: Cron 定期运行，检测看板死锁并自动恢复
# 触发条件: blocked>0 且 running==0 / blocked>5 / 连续崩溃>3
# =============================================================================
set -euo pipefail

BOARD="privateclouddrive"
PROJECT_DIR="D:/Devs/Projects/Personal/PrivateCloudDrive"
GUARD_SCRIPT="$PROJECT_DIR/scripts/git-workspace-guard.sh"
LOG_FILE="$PROJECT_DIR/docs/validation/watchdog-$(date +%Y%m%d).log"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
log()  { echo -e "${GREEN}[$(date +%H:%M:%S)]${NC} $*" | tee -a "$LOG_FILE"; }
warn() { echo -e "${YELLOW}[$(date +%H:%M:%S)]${NC} $*" | tee -a "$LOG_FILE"; }
err()  { echo -e "${RED}[$(date +%H:%M:%S)]${NC} $*" | tee -a "$LOG_FILE"; }

mkdir -p "$(dirname "$LOG_FILE")"

# ── 获取看板统计 ──
get_stats() {
    hermes kanban --board "$BOARD" stats 2>/dev/null || echo "STATS_FAILED"
}

# ── 解析统计数字 ──
parse_stat() {
    local stats="$1" key="$2"
    echo "$stats" | grep "^  $key" | grep -oP '\d+' | head -1 || echo "0"
}

# ── 获取诊断信息 ──
get_diagnostics() {
    hermes kanban --board "$BOARD" diagnostics 2>/dev/null || echo ""
}

# ── 统计崩溃任务数 ──
count_crashes() {
    local diag="$1"
    echo "$diag" | grep -c "repeated_crashes" || echo "0"
}

# ── 核恢复流程 ──
nuclear_recovery() {
    log "============= 核恢复启动 ============="

    # 1. 检查并修复 Git
    if [ -f "$GUARD_SCRIPT" ]; then
        bash "$GUARD_SCRIPT" || {
            err "Git 守卫自愈失败"
            return 1
        }
    else
        warn "Git 守卫脚本不存在，跳过"
    fi

    # 2. 传播技能
    local ROOT='C:/Users/q4528/AppData/Local/hermes'
    for p in chief-of-staff pm devops-eng mobile-eng qa-eng backend-eng frontend-eng api-contract-eng test-automation-eng identity-auth-eng db-dba sre-observability; do
        mkdir -p "$ROOT/profiles/$p/skills/software-development" "$ROOT/profiles/$p/skills/devops" 2>/dev/null
        cp -R "$ROOT/skills/software-development/privateclouddrive-delivery" "$ROOT/profiles/$p/skills/software-development/" 2>/dev/null
        cp -R "$ROOT/skills/devops/docker-compose-operations" "$ROOT/profiles/$p/skills/devops/" 2>/dev/null
    done
    log "技能已传播到所有 profile"

    # 3. 获取 blocked 任务列表并解锁
    local blocked_list
    blocked_list=$(hermes kanban --board "$BOARD" list --status blocked 2>/dev/null | grep "^⊘" | grep -oP 't_\w+' || echo "")

    if [ -z "$blocked_list" ]; then
        log "没有阻塞任务"
        return 0
    fi

    local count
    count=$(echo "$blocked_list" | wc -l)
    warn "发现 $count 个阻塞任务，准备解锁..."

    for tid in $blocked_list; do
        # 先 reclaim（清除残留 claim）
        hermes kanban --board "$BOARD" reclaim "$tid" --reason "看门狗自动回收" 2>/dev/null || true
        # 再 unblock
        hermes kanban --board "$BOARD" unblock "$tid" 2>/dev/null && log "解锁: $tid"
    done

    # 4. 派发
    hermes kanban --board "$BOARD" dispatch --max 8 2>/dev/null
    log "已派发任务"

    log "============= 核恢复完成 ============="
}

# ── 轻度恢复（仅崩溃任务 reclaim + redispatch） ──
light_recovery() {
    log "============= 轻度恢复启动 ============="

    local diag
    diag=$(get_diagnostics)
    local crash_count
    crash_count=$(count_crashes "$diag")

    if [ "$crash_count" -eq 0 ]; then
        log "无崩溃任务"
        return 0
    fi

    warn "检测到 $crash_count 个崩溃任务"

    # 提取崩溃任务 ID
    local crash_ids
    crash_ids=$(echo "$diag" | grep -oP 't_\w+' | sort -u)

    for tid in $crash_ids; do
        hermes kanban --board "$BOARD" reclaim "$tid" --reason "看门狗自动回收崩溃任务" 2>/dev/null && log "回收: $tid"
        hermes kanban --board "$BOARD" unblock "$tid" 2>/dev/null && log "解锁: $tid"
    done

    hermes kanban --board "$BOARD" dispatch --max 4 2>/dev/null
    log "轻度恢复完成"
}

# ── 主流程 ──
main() {
    log "看门狗巡检开始"

    # 获取当前状态
    local stats
    stats=$(get_stats)

    if [ "$stats" = "STATS_FAILED" ]; then
        err "无法获取看板统计"
        exit 1
    fi

    local blocked running ready done
    blocked=$(parse_stat "$stats" "blocked")
    running=$(parse_stat "$stats" "running")
    ready=$(parse_stat "$stats" "ready")
    done_count=$(parse_stat "$stats" "done")

    log "状态: $running 运行中 / $ready 就绪 / $blocked 阻塞 / $done_count 已完成"

    # ═══════════════════════════════════════════
    # 触发规则
    # ═══════════════════════════════════════════

    # 规则1: 死锁 (全部阻塞，无运行中，无就绪)
    if [ "$blocked" -gt 0 ] && [ "$running" -eq 0 ] && [ "$ready" -eq 0 ]; then
        err "!! 检测到死锁: blocked=$blocked running=0 ready=0"
        nuclear_recovery
        exit 0
    fi

    # 规则2: 大量阻塞 (> 50%)
    local total=$((blocked + running + ready))
    if [ "$total" -gt 0 ]; then
        local blocked_pct=$((blocked * 100 / total))
        if [ "$blocked_pct" -gt 50 ]; then
            warn "!! 阻塞率过高: $blocked_pct% ($blocked/$total)"
            nuclear_recovery
            exit 0
        fi
    fi

    # 规则3: 崩溃任务
    local diag
    diag=$(get_diagnostics)
    local crash_count
    crash_count=$(count_crashes "$diag")

    if [ "$crash_count" -gt 3 ]; then
        warn "!! 崩溃任务过多: $crash_count"
        light_recovery
    elif [ "$crash_count" -gt 0 ]; then
        log "小量崩溃: $crash_count 个任务 (暂不触发恢复)"
    fi

    # 正常时静默，避免 cron 每 30 分钟推送噪音
    # 如需查看状态: hermes kanban --board privateclouddrive stats
}

main "$@"
