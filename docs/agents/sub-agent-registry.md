# Sub-Agent Registry Template

> 运行时由主智能体维护的智能体注册表。每个工作流会话初始化一份。

---

## Registry Format

```markdown
# Agent Registry — {session_id}

## Session Info
- Started: {datetime}
- Feature: {feature_name}
- PRD: {prd_path}
- Issues: {issue_numbers}

## Agents

### Layer 0 (No Dependencies)
| Name | AgentId | Role | Issue | Status | Started | Completed | Files |
|------|---------|------|-------|--------|---------|-----------|-------|
| dev-1 | {id} | dev | #1 | done | {time} | {time} | file1.cs, file2.cs |
| dev-2 | {id} | dev | #2 | running | {time} | - | - |

### Layer 1 (Depends on Layer 0)
| Name | AgentId | Role | Issue | Status | Started | Completed | Files |
|------|---------|------|-------|--------|---------|-----------|-------|
| dev-3 | {id} | dev | #3 | created | - | - | - |

## Test Agents
| Name | AgentId | Role | Issue | Status | Findings | Bugs |
|------|---------|------|-------|--------|----------|------|
| test-1 | {id} | test | #1 | done | 0 | - |
| test-2 | {id} | test | #2 | running | 1 | BUG-001 |

## Fix Rounds
| Bug ID | Source Test | Fix Agent | Round | Status | Verified By |
|--------|-----------|-----------|-------|--------|-------------|
| BUG-001 | test-2 | dev-2 (fix-2-retry1) | 1/3 | verified | test-2 |

## Escalations
| Bug ID | Rounds Attempted | Last Fix Agent | Reason |
|--------|-----------------|----------------|--------|
| - | - | - | - |
```

---

## Status Values

| Status | Meaning |
|--------|---------|
| `created` | 已注册，未启动 |
| `running` | 正在执行 |
| `done` | 成功完成 |
| `failed` | 执行失败 |
| `verified` | 修复已验收 |
| `still_open` | 修复未通过验收 |
| `escalated` | 超过 3 轮，等待人工 |

---

## Agent Lifecycle

```
                    ┌──────────┐
                    │ created  │
                    └────┬─────┘
                         │ Agent()
                         ▼
                    ┌──────────┐
              ┌─────│ running  │─────┐
              │     └──────────┘     │
              │                      │
              ▼                      ▼
         ┌──────────┐         ┌──────────┐
         │   done   │         │  failed  │
         └──────────┘         └────┬─────┘
                                   │ SendMessage(fix)
                                   ▼
                              ┌──────────┐
                              │ running  │ (retry)
                              └────┬─────┘
                                   │
                              ┌────┴────┐
                              ▼         ▼
                         ┌──────┐  ┌──────────┐
                         │ done │  │ escalated│ (round >= 3)
                         └──────┘  └──────────┘
```

---

## Registry Operations

### Init (Phase 1)
```
AGENT_REGISTRY = { agents: [], bugs: [], escalations: [] }
for issue in issues:
    AGENT_REGISTRY.agents.append({
        name: "dev-{issue.number}",
        role: "dev",
        issue: issue.number,
        status: "created"
    })
```

### Register Agent (Phase 2/3)
```
agent = Agent(name="dev-1", ...)
AGENT_REGISTRY.find("dev-1").agentId = agent.agentId
AGENT_REGISTRY.find("dev-1").status = "running"
```

### Complete Agent (Notification)
```
on_agent_complete(name):
    AGENT_REGISTRY.find(name).status = "done"
    AGENT_REGISTRY.find(name).completed = now()
```

### Register Bug (Phase 3)
```
on_bug_found(test_name, bug):
    AGENT_REGISTRY.bugs.append({
        id: "BUG-{seq}",
        source_test: test_name,
        source_dev: test_name.replace("test-", "dev-"),
        description: bug.description,
        status: "open",
        rounds: 0
    })
```

### Fix Bug (Phase 4)
```
on_fix_bug(bug):
    bug.rounds += 1
    dev_name = bug.source_dev
    SendMessage(to=dev_name, message="修复: {bug.description}")
    # 等待 dev 完成后:
    SendMessage(to=bug.source_test, message="验收: {bug.description}")
```

### Escalate (Phase 4, round >= 3)
```
on_escalate(bug):
    bug.status = "escalated"
    AGENT_REGISTRY.escalations.append(bug)
```
