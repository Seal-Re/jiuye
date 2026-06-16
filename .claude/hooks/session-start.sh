#!/bin/bash
# Claude Code SessionStart hook: Load project context at session start
# Outputs context information that Claude sees when a session begins
#
# Input schema (SessionStart): No stdin input

echo "=== Claude Code Game Studios — Session Context ==="

# Current branch
BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ -n "$BRANCH" ]; then
    echo "Branch: $BRANCH"

    # Recent commits
    echo ""
    echo "Recent commits:"
    git log --oneline -5 2>/dev/null | while read -r line; do
        echo "  $line"
    done
fi

# Current sprint (find most recent sprint file)
LATEST_SPRINT=$(ls -t production/sprints/sprint-*.md 2>/dev/null | head -1)
if [ -n "$LATEST_SPRINT" ]; then
    echo ""
    echo "Active sprint: $(basename "$LATEST_SPRINT" .md)"
fi

# Current milestone
LATEST_MILESTONE=$(ls -t production/milestones/*.md 2>/dev/null | head -1)
if [ -n "$LATEST_MILESTONE" ]; then
    echo "Active milestone: $(basename "$LATEST_MILESTONE" .md)"
fi

# Open bug count
BUG_COUNT=0
for dir in tests/playtest production; do
    if [ -d "$dir" ]; then
        count=$(find "$dir" -name "BUG-*.md" 2>/dev/null | wc -l)
        BUG_COUNT=$((BUG_COUNT + count))
    fi
done
if [ "$BUG_COUNT" -gt 0 ]; then
    echo "Open bugs: $BUG_COUNT"
fi

# Code health quick check
if [ -d "src" ]; then
    TODO_COUNT=$(grep -r "TODO" src/ 2>/dev/null | wc -l)
    FIXME_COUNT=$(grep -r "FIXME" src/ 2>/dev/null | wc -l)
    if [ "$TODO_COUNT" -gt 0 ] || [ "$FIXME_COUNT" -gt 0 ]; then
        echo ""
        echo "Code health: ${TODO_COUNT} TODOs, ${FIXME_COUNT} FIXMEs in src/"
    fi
fi

# --- Active session state recovery ---
STATE_FILE="production/session-state/active.md"
if [ -f "$STATE_FILE" ]; then
    echo ""
    echo "=== ACTIVE SESSION STATE DETECTED ==="
    echo "A previous session left state at: $STATE_FILE"
    echo "Read this file to recover context and continue where you left off."
    echo ""
    echo "Quick summary (last 20 lines):"
    tail -20 "$STATE_FILE" 2>/dev/null
    TOTAL_LINES=$(wc -l < "$STATE_FILE" 2>/dev/null)
    if [ "$TOTAL_LINES" -gt 20 ]; then
        echo "  ... ($TOTAL_LINES total lines — read the full file to continue)"
    fi
    echo "=== END SESSION STATE PREVIEW ==="
fi

# --- Recommended Skills (A.9: 主动调度 skill) ---
# 根据当前 Production 阶段 + sprint 状态动态推荐 skill，让 Claude 无需手动提示即知道用哪个
STAGE=$(cat production/stage.txt 2>/dev/null | tr -d '[:space:]')
WIP_STORY=$(grep 'status: in-progress' production/sprint-status.yaml 2>/dev/null | wc -l | tr -d '[:space:]')
READY_STORY=$(grep 'status: ready-for-dev' production/sprint-status.yaml 2>/dev/null | wc -l | tr -d '[:space:]')

echo ""
echo "=== 推荐 Skills（红线 A.9：主动调度）==="
echo "阶段: ${STAGE:-Unknown} | WIP: ${WIP_STORY} | 待开发: ${READY_STORY}"
echo ""
echo "  对账/状态:"
echo "    /sprint-status     — 查当前 sprint 进度（快速）"
echo "    /scope-check       — 检查范围蔓延"
echo ""
echo "  开发流程:"
if [ "${READY_STORY}" -gt 0 ] 2>/dev/null; then
    echo "  ★ /dev-story       — 实现 story（有 ${READY_STORY} 个 ready-for-dev）"
fi
echo "    /story-readiness   — 验证 story 是否 implementation-ready"
echo "    /smoke-check       — 提交前冒烟 gate"
echo "    /code-review       — 代码质量审查"
echo ""
echo "  设计/平衡:"
echo "    /design-system     — 补 GDD（combat/cultivation 等）"
echo "    /balance-check     — 验证战斗/经济平衡"
echo "    /balance-check     — 需 design/balance/ 数据文件"
echo ""
echo "  工具:"
echo "    /sprint-plan       — 更新 sprint 计划"
echo "    /help              — 根据当前状态推荐下一步"
echo ""
echo "  SunshineFlow AI 出图:"
echo "    /sunshineflow-banana-alpha — AI生成像素资产(装备/图标/立绘)"
echo "    工具脚本: tools/pixel-pipeline/  AI相关文档: tools/pixel-pipeline/AIGEN_TOOL.md"
echo "==================================="

exit 0
