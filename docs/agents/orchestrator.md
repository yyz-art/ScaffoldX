# Multi-Agent Orchestration System

> 本文档定义 ScaffoldX 项目的多智能体编排规范。主智能体（Claude）作为编排者，按本规范调度子智能体完成批量开发、测试、修复循环。

---

## 1. Core Principles

| # | 原则 | 说明 |
|---|------|------|
| C1 | **谁写谁修** | 子智能体 A 写的代码出了 bug，必须由 A 来修复，不得转嫁 |
| C2 | **谁提谁验** | 测试智能体 T 提出的 bug，必须由 T 来验收修复结果 |
| C3 | **三轮上限** | 修复循环最多 3 轮。3 轮后仍有失败，暂停等待人工指导 |
| C4 | **最小上下文** | 每个子智能体只接收完成任务所需的最少信息，不传递无关上下文 |
| C5 | **可恢复性** | 通过 agent name 追踪每个子智能体，可用 SendMessage 恢复对话 |
| C6 | **日志驱动** | 每轮循环产出日志，最终产出总结报告 |
| C7 | **批量并行** | 独立任务并行启动，有依赖关系的串行等待 |

---

## 2. Agent ID Collection（智能体注册表）

### 2.1 ID 命名规则

```
{role}-{issue_number}-{variant}
```

| 字段 | 说明 | 示例 |
|------|------|------|
| role | 角色缩写 | `dev`, `test`, `fix`, `review` |
| issue_number | 对应 issue 编号 | `1`, `2`, `3` |
| variant | 变体标识（可选） | `a`, `b`, `retry1` |

**示例：**
- `dev-1` — Issue #1 的开发智能体
- `test-1` — Issue #1 的测试智能体
- `fix-1-retry1` — Issue #1 的第 1 轮修复智能体
- `test-2a` — Issue #2a 的测试智能体（拆分场景）

### 2.2 Agent Registry（运行时维护）

主智能体在每轮工作流开始时初始化注册表，格式如下：

```
AGENT_REGISTRY:
- dev-1: { agentId: "<id>", status: "running|done|failed", issue: 1, files: [...] }
- test-1: { agentId: "<id>", status: "running|done|failed", issue: 1, findings: [...] }
- dev-2: { agentId: "<id>", status: "running|done|failed", issue: 2, files: [...] }
...
```

**状态机：**
```
created → running → done
                  → failed → (retry: fix-{n}-retry{r}) → running → done
                                                                  → failed (r≥3) → escalate
```

### 2.3 ID 使用规则

| 场景 | 操作 |
|------|------|
| 启动新智能体 | `Agent(name="{id}", ...)` |
| 恢复已有智能体 | `SendMessage(to="{id}", ...)` |
| 查询智能体状态 | 检查 AGENT_REGISTRY |
| 修复 bug | `SendMessage(to="dev-{issue}", ...)` — 恢复原开发者 |
| 验收修复 | `SendMessage(to="test-{issue}", ...)` — 恢复原测试者 |

---

## 3. Project Initialization（项目初始化）

在执行工作流前，主智能体必须完成以下初始化：

### 3.1 读取上下文

```
1. 读取 CONTEXT.md — 领域术语
2. 读取 docs/adr/ — 架构决策
3. 读取 CLAUDE.md — 项目规范
4. 读取 docs/agents/triage-labels.md — 标签规范
5. 读取 docs/agents/issue-tracker.md — Issue 操作规范
```

### 3.2 技能调用顺序

按 Matt Pocock 推荐顺序：

```
/grill-with-docs  → 需求澄清，更新 CONTEXT.md 和 ADR
/to-prd           → 生成 PRD
/to-issues        → 拆分为 issues
/tdd              → 测试驱动开发
/diagnose         → 问题诊断
/improve-codebase-architecture → 架构改进（每隔几天跑一次）
```

### 3.3 初始化检查清单

- [ ] CONTEXT.md 存在且内容完整
- [ ] docs/adr/ 包含相关 ADR
- [ ] GitHub Issues 可访问（`gh auth status`）
- [ ] 测试可运行（`dotnet test` 通过）
- [ ] AGENT_REGISTRY 已初始化（空表）

---

## 4. Main Agent Workflow（主智能体工作流）

### 4.1 完整流程图

```
┌─────────────────────────────────────────────────────────────┐
│ Phase 1: PLAN                                               │
│   /grill-with-docs → /to-prd → /to-issues                  │
│   输出: issues 列表 + 依赖图                                  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 2: BATCH DEVELOP                                      │
│   按依赖顺序并行启动 dev-{n} 智能体                           │
│   每个 dev-{n} 使用 /tdd 工作流                              │
│   等待所有 dev-{n} 完成                                       │
│   产出: 代码变更 + 测试通过                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 3: BATCH TEST                                         │
│   并行启动 test-{n} 智能体（每个对应一个 dev-{n}）             │
│   每个 test-{n} 运行 /diagnose + 测试验证                    │
│   收集所有 test-{n} 的 findings                              │
│   产出: bug 列表（每个 bug 标记 dev-{n} 来源）                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 4: FIX LOOP (max 3 rounds)                            │
│                                                             │
│   for round in 1..3:                                        │
│     if no bugs: break                                       │
│                                                             │
│     for each bug in bugs:                                   │
│       1. SendMessage(to="dev-{n}") — 恢复原开发者修复         │
│       2. SendMessage(to="test-{n}") — 恢复原测试者验收        │
│                                                             │
│     if all verified: break                                  │
│     else: continue to next round                            │
│                                                             │
│   if round == 3 and still bugs:                             │
│     ESCALATE — 等待人工指导                                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 5: COMMIT & LOG                                       │
│   git commit — 提交本轮所有变更                               │
│   生成 WORKFLOW_LOG_{date}.md                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ Phase 6: SUMMARY REPORT                                     │
│   汇总所有轮次数据                                            │
│   输出最终报告                                               │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Phase 1: PLAN（详细）

```
1. 用户输入需求描述
2. 主智能体调用 /grill-with-docs 澄清需求
   - 更新 CONTEXT.md（新术语）
   - 创建/更新 docs/adr/（新决策）
3. 主智能体调用 /to-prd 生成 PRD
   - 输出: docs/prd-{feature-name}.md
4. 主智能体调用 /to-issues 拆分 issues
   - 输出: GitHub Issues（带 ready-for-agent 标签）
   - 记录依赖关系图
5. 主智能体初始化 AGENT_REGISTRY
```

### 4.3 Phase 2: BATCH DEVELOP（详细）

```
1. 拓扑排序 issues 依赖图
2. 按层级并行启动:

   Layer 0 (无依赖):
     Agent(name="dev-0", prompt=..., run_in_background=true)
     Agent(name="dev-1", prompt=..., run_in_background=true)

   Layer 1 (依赖 Layer 0):
     等待 Layer 0 全部完成
     Agent(name="dev-2", prompt=..., run_in_background=true)
     ...

3. 每个 dev-{n} 的 prompt 包含:
   - 任务描述（从 issue body 提取）
   - 约束条件（从 CLAUDE.md 提取）
   - 关键文件路径
   - TDD 要求: 先写测试，再实现

4. 等待所有 dev-{n} 完成通知
5. 更新 AGENT_REGISTRY 状态
```

### 4.4 Phase 3: BATCH TEST（详细）

```
1. 对每个已完成的 dev-{n}:
   Agent(name="test-{n}", prompt=..., run_in_background=true)

2. 每个 test-{n} 的 prompt 包含:
   - "检查 dev-{n} 的实现"
   - 运行相关测试
   - 运行 /diagnose 诊断
   - 输出 findings 列表

3. 等待所有 test-{n} 完成通知
4. 收集 findings，按 dev-{n} 分组
5. 更新 AGENT_REGISTRY
```

### 4.5 Phase 4: FIX LOOP（详细）

```
for round in 1..3:
    bugs = collect_all_bugs()
    if bugs.is_empty:
        break

    for bug in bugs:
        dev_id = bug.source_dev    # e.g. "dev-1"
        test_id = bug.source_test  # e.g. "test-1"

        # 步骤 1: 恢复原开发者修复
        SendMessage(to=dev_id, message="""
            修复以下 bug:
            - 描述: {bug.description}
            - 文件: {bug.file}:{bug.line}
            - 期望: {bug.expected}
            - 实际: {bug.actual}
            修复后运行测试确认通过。
        """)

        # 步骤 2: 恢复原测试者验收
        SendMessage(to=test_id, message="""
            验收修复:
            - 原始 bug: {bug.description}
            - 修复方案: {dev_id 的修复内容}
            运行测试，确认 bug 已解决且无回归。
        """)

        # 更新状态
        if test_id 验收通过:
            bug.status = "verified"
        else:
            bug.status = "still_open"

    # 提交本轮修复
    git commit -m "fix round {round}"

if round == 3 and has_open_bugs():
    print("⚠️ 3 轮修复后仍有未解决的 bug，等待人工指导:")
    for bug in open_bugs:
        print(f"  - {bug.id}: {bug.description}")
    STOP — 等待用户输入
```

### 4.6 Phase 5: COMMIT & LOG（详细）

每轮循环结束后生成日志：

```markdown
# Workflow Log — {date}

## Phase: {phase_name}
## Round: {round_number}

### Issues Processed
| Issue | Dev Agent | Status | Files Changed |
|-------|-----------|--------|---------------|
| #1 | dev-1 | done | file1.cs, file2.cs |
| #2 | dev-2 | done | file3.cs |

### Test Results
| Issue | Test Agent | Findings | Status |
|-------|-----------|----------|--------|
| #1 | test-1 | 0 | pass |
| #2 | test-2 | 1 | bug found |

### Fix Rounds
| Bug | Source | Fix Agent | Rounds | Status |
|-----|--------|-----------|--------|--------|
| BUG-001 | test-2 | dev-2 | 1 | verified |

### Commits
- abc1234 feat: issue #1 implementation
- def5678 fix: issue #2 bug fix round 1
```

### 4.7 Phase 6: SUMMARY REPORT（详细）

全部完成后生成总结：

```markdown
# Orchestration Summary — {date}

## Overview
- Total issues: {n}
- Successful: {n}
- Escalated: {n}
- Total fix rounds: {n}
- Total commits: {n}

## Per-Issue Summary
| Issue | Title | Dev | Test | Fix Rounds | Final Status |
|-------|-------|-----|------|------------|--------------|
| #1 | ... | dev-1 | test-1 | 0 | First-pass |
| #2 | ... | dev-2 | test-2 | 2 | Fixed |
| #3 | ... | dev-3 | test-3 | 3 | Escalated |

## Agent Performance
| Agent | Tasks | Success Rate | Avg Fix Rounds |
|-------|-------|-------------|----------------|
| dev-1 | 1 | 100% | 0 |
| dev-2 | 1 | 100% | 2 |

## Files Changed
- Total files: {n}
- Lines added: {n}
- Lines removed: {n}

## Test Results
- Total tests: {n}
- Passed: {n}
- Failed: {n}

## Lessons Learned
- {observations}
```

---

## 5. Sub-Agent Template（子智能体模板）

### 5.1 Developer Agent Template

```yaml
name: "dev-{issue_number}"
description: "Issue #{issue_number} 开发智能体"
subagent_type: "tdd-guide"
mode: "auto"
run_in_background: true

prompt: |
  ## Task
  {从 issue body 提取的任务描述}

  ## Constraints
  - Language: C# (.NET 10, WPF)
  - Test framework: xUnit + FluentAssertions + Moq
  - Working directory: D:\Ver pro\ScaffoldX
  - Follow TDD: write tests first (RED), implement (GREEN), refactor (IMPROVE)
  - 现有测试不能回归

  ## Key Files
  {从 issue body 提取的关键文件列表}

  ## Acceptance Criteria
  {从 issue body 提取的验收标准}
```

### 5.2 Tester Agent Template

```yaml
name: "test-{issue_number}"
description: "Issue #{issue_number} 测试智能体"
subagent_type: "tdd-guide"
mode: "auto"
run_in_background: true

prompt: |
  ## Task
  验证 Issue #{issue_number} 的实现是否正确。

  ## What to Check
  1. 运行 `dotnet test tests/ScaffoldX.Core.Tests/` — 确认所有测试通过
  2. 运行 `dotnet test tests/ScaffoldX.App.Tests/` — 确认所有测试通过
  3. 检查 dev-{issue_number} 修改的文件:
     {列出 dev 修改的文件}
  4. 运行 /diagnose 诊断逻辑问题
  5. 检查验收标准是否全部满足

  ## Output Format
  输出 findings 列表，每个 finding 格式:
  - **BUG-{id}**: {description}
    - File: {file}:{line}
    - Expected: {expected}
    - Actual: {actual}
    - Severity: CRITICAL|HIGH|MEDIUM|LOW

  如果没有问题，输出: "All checks passed."
```

### 5.3 Fix Agent Template

```yaml
name: "fix-{issue_number}-retry{round}"
description: "Issue #{issue_number} 第 {round} 轮修复"
subagent_type: "tdd-guide"
mode: "auto"
run_in_background: true

prompt: |
  ## Bug to Fix
  {bug.description}

  ## Location
  File: {bug.file}:{bug.line}

  ## Expected vs Actual
  - Expected: {bug.expected}
  - Actual: {bug.actual}

  ## Context
  原始实现由 dev-{issue_number} 完成。
  修改文件: {dev 修改的文件列表}

  ## Instructions
  1. 定位 bug 根因
  2. 编写失败测试重现 bug
  3. 修复代码使测试通过
  4. 运行全量测试确认无回归
  5. 输出修复方案摘要
```

---

## 6. Recovery Mechanism（恢复机制）

### 6.1 Claude Code Sub-Agent Recovery

参考 Claude Code 官方文档，通过 `SendMessage` 恢复子智能体：

```
# 启动时记录 name
Agent(name="dev-1", ...) → agentId: "abc123"

# 恢复时使用 name
SendMessage(to="dev-1", message="继续工作...")
```

### 6.2 Recovery Scenarios

| 场景 | 操作 |
|------|------|
| 智能体超时 | 检查 AGENT_REGISTRY，SendMessage 催促 |
| 智能体失败 | SendMessage 发送修复指令 |
| 会话中断 | 重新读取 AGENT_REGISTRY，SendMessage 恢复所有 running 状态的智能体 |
| 部分完成 | 对 done 状态跳过，对 running/failed 状态恢复 |

### 6.3 Registry Persistence

AGENT_REGISTRY 在每轮操作后持久化到内存（会话内）。如果会话中断：

1. 重新读取 docs/agents/orchestrator.md
2. 根据 git log 重建已完成的状态
3. 对未完成的任务重新启动智能体

---

## 7. Error Handling（错误处理）

### 7.1 智能体启动失败

```
if Agent() 返回错误:
    记录到 AGENT_REGISTRY (status: "failed")
    等待 5 秒后重试 1 次
    如果仍失败: 标记为 "escalate"
```

### 7.2 测试回归

```
if 运行测试发现回归:
    定位回归引入的 commit
    恢复对应的 dev-{n} 修复回归
    回归修复不计入 3 轮限制（回归是新 bug）
```

### 7.3 冲突处理

```
if 两个 dev-{n} 修改了同一个文件:
    先完成的 commit，后完成的 rebase
    如果 rebase 冲突: 恢复后完成的 dev-{n} 解决冲突
```

---

## 8. Usage Example（使用示例）

```
用户: "实现 Issue #1 的主题切换功能"

主智能体:
1. 读取 orchestrator.md
2. 初始化 AGENT_REGISTRY
3. 读取 Issue #1 body
4. 启动 dev-1:
   Agent(name="dev-1", prompt="实现 Issue #1...", run_in_background=true)
5. 等待 dev-1 完成通知
6. 启动 test-1:
   Agent(name="test-1", prompt="验证 Issue #1...", run_in_background=true)
7. 等待 test-1 完成通知
8. 如果 test-1 报告 bug:
   SendMessage(to="dev-1", message="修复 BUG-001...")
   SendMessage(to="test-1", message="验收 BUG-001...")
9. 重复 8 直到通过或达到 3 轮
10. git commit
11. 生成 WORKFLOW_LOG
12. 生成 SUMMARY_REPORT
```
