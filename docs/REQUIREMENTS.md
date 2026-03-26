# CodeFabric — Requirements

## Purpose

CodeFabric is a .NET console orchestrator that automates a phase-gated feature delivery workflow. It coordinates GitHub Copilot CLI agents through three sequential phases — **Design**, **Implementation**, and **Test** — with human approval checkpoints between phases.

## Workflow Structure

### Phases and Steps

**Phase 1: Design**
- High Level Design (work)
- Review High Level Design (review)
- Detailed Design (work)
- Review Detailed Design (review)

**Phase 2: Implementation**
- Code Implementation (work)
- Unit Testing (work)
- Code Review (review)

**Phase 3: Test**
- Integration Testing (work)
- Review Integration Testing (review)
- User Acceptance Testing (work)
- Review UAT (review)

### Step Types

- **Work**: Agent produces an artifact (Markdown, code, etc.)
- **Review**: Agent evaluates prior artifact and returns JSON verdict + remarks

## Execution Model

1. Execute steps sequentially within each phase.
2. Each step spawns `copilot.exe` with a prompt template and awaits completion.
3. Success = exit code 0 AND output artifact exists AND HANDOUT block outputted to stdout.
4. After each review, evaluate if more iteration is needed. Evaluation is based on the severity of remarks and a configurable threshold.
5. When no more iterations are needed, or max iterations reached, ask for approval to move to the next phase.
   - Human approval is required in first version, but design should allow for future agent-driven approval.

## Iterative Refinement

- Within a phase, work and review steps loop iteratively.
- Each step output has outputs HANDOUT block
- The prompt for the next interation refers to the output of previous interations and contains context built base on the HANDOUT block.
- Loop terminates when review verdict is "pass" OR all remarks are severity "minor" or below (configurable threshold).
- If max iterations reached without passing, escalate to human.

## Resumability

- Persist workflow state. JSON file (`.orchestrator/state.json`) in first version, with potential for SQLite in future.
- On startup, detect current phase and resume from the last incomplete step. If in the middle of a step, restart that step.

## Human Approval Gates

- After all steps in a phase complete, orchestrator prompts human for approval.
- Human can approve, reject with remarks which imply more iterations.
- Approved phases are immutable — their artifacts not re-run.

## Constraints (v1)

- Sequential step execution only (no parallelism).
- Deterministic loop termination (machine-readable review verdicts).
- CLI-based human approval (not agent-driven).
- No external system integration (Jira, Teams, email).
