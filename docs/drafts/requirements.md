# Workflow Orchestrator — Refined Requirements
**Version:** 1.0  
**Status:** Draft
**Runtime:** C# Console App

---

## 1. Purpose

The Orchestrator is a C# console application that automates a structured, phase-gated feature delivery workflow. It coordinates specialised GitHub Copilot CLI agents across three sequential phases — **Design**, **Implementation**, and **Test** — managing agent invocation, artifact validation, iterative refinement loops, and human approval gates between phases.

The workflow already exists as a manual engineering practice. This tool automates its execution while preserving human oversight at phase boundaries.

---

## 2. Scope

### In Scope (v1)
- Sequential execution of all defined phases and steps.
- Spawning GitHub Copilot CLI agent processes via `copilot.exe` (prompt via stdin).
- Detecting step completion via exit code and output artifact presence.
- Iterative step loops with configurable retry caps per phase.
- Phase-level human approval via CLI prompt (Y/N with optional remarks).
- Resumability: detecting existing artifacts on startup and skipping completed steps.
- Persisting workflow state to a JSON file in the repository.
- Structured console output with phase, step, and iteration context.

### Out of Scope (v1)
- Agent-driven approval (future version).
- Non-deterministic loop termination strategies (future version).
- Parallel step execution.
- Integration with external systems (Jira, Teams, email notifications).
- GUI or web-based approval interface.

---

## 3. Workflow Structure

In future versions, we may want to support multiple workflow types (e.g., feature delivery, incident response) with different phases and steps. To enable this without modifying the engine, all workflow-specific knowledge is encapsulated in a Workflow Definition.

### 3.1 Phases and Steps

The workflow is organised into incremental phases. Each phase contains one or more **work steps** and one or more **review steps**. Inside a phase is iterative: at any iteration any previous work may be modified to improve the final result.

In contracts phases are incremental. Once a phase is completed and approved, its artifacts are **immutable** — the orchestrator will not re-run or modify them in subsequent phases. This ensures a clear progression.

```
Phase 1: Design
  Step 1.1 — High Level Design          [Work]
  Step 1.2 — Review High Level Design   [Review]
  Step 1.3 — Detailed Design            [Work]
  Step 1.4 — Review Detailed Design     [Review]

Phase 2: Implementation
  Step 2.1 — Code Implementation        [Work]
  Step 2.2 — Unit Testing               [Work]
  Step 2.3 — Code Review                [Review]

Phase 3: Test
  Step 3.1 — Integration Testing        [Work]
  Step 3.2 — Review Integration Testing [Review]
  Step 3.3 — User Acceptance Testing    [Work]
  Step 3.4 — Review UAT                 [Review]
```

### 3.2 Step Types

| Type | Description |
|---|---|
| **Work** | Executes an agent that produces a new artifact. |
| **Review** | Executes an agent that evaluates the preceding work artifact and produces a review artifact containing remarks and a pass/fail verdict. |

### 3.3 Phase Gate

After all steps in a phase complete successfully, the orchestrator **pauses and prompts the human** for phase approval before proceeding to the next phase.

Once a phase is approved, its artifacts are **immutable** — the orchestrator will not re-run or modify them in subsequent phases.

---

## 4. Iterative Loop Behaviour

### 4.1 Within a Phase

Each phase executes as an iterative loop:

1. Run the work step → produce artifact.
2. Run the review step → produce review artifact (verdict + remarks).
3. Evaluate the review verdict:
   - **Pass / remarks below severity threshold** → exit loop, proceed to phase gate.
   - **Fail / remarks above threshold** → increment iteration counter, re-run work step with review remarks injected into the agent prompt, repeat from step 2.
4. If iteration count reaches the configured maximum → escalate to human (see §6.3).

### 4.2 Loop Termination (v1 — Deterministic)

In v1, loop termination is deterministic. The review artifact must contain a machine-readable verdict field. The orchestrator reads this field to decide whether to continue iterating.

**Review artifact verdict contract (v1):**
```json
{
  "verdict": "pass" | "fail",
  "remarks": [ { "severity": "critical" | "major" | "minor", "description": "..." } ]
}
```

A loop is considered complete when:
- `verdict` is `"pass"`, **OR**
- All remarks have severity `"minor"` only (configurable threshold per phase).

### 4.3 Remarks Injection

When a loop iterates, the orchestrator constructs the next work step prompt by appending the full remarks list from the review artifact to the original step prompt template. The agent is therefore always given both the original instructions and the accumulated feedback.

---

## 5. Resumability

### 5.1 State File

The orchestrator persists its state to a JSON file at a well-known path within the repository:

```
<repo-root>/.orchestrator/state.json
```

The state file records the current phase, current step, current iteration, and the completion status and artifact path of every step that has been executed.

### 5.2 Startup Behaviour

On startup, the orchestrator:

1. Reads the state file (if present).
2. For each previously completed step, verifies that the expected output artifact exists on disk.
3. If an artifact is present and the step is marked complete in state, the step is **skipped**.
4. If an artifact is present but the step is not marked complete (e.g. crash mid-write), the orchestrator treats it as incomplete and re-runs the step.
5. Execution resumes from the first incomplete step.

This means a workflow interrupted after a review step will correctly resume by reading the existing review remarks and applying them to the next iteration of the work step.

### 5.3 Phase Approval State

Phase approvals are also persisted in the state file. A phase that was already approved will not prompt the human again on resume.

---

## 6. Agent Invocation Contract

### 6.1 Process Invocation

Each step invokes `copilot.exe` as a child process. The prompt is written to the process's **stdin**. The orchestrator waits synchronously for the process to exit.

```csharp
// Pseudocode — illustrative
var process = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "copilot.exe",
        Arguments = $"--agent={agentName}",
        RedirectStandardInput = true,
        UseShellExecute = false
    }
};
process.Start();
process.StandardInput.Write(builtPrompt);
process.StandardInput.Close();
process.WaitForExit();
```

### 6.2 Success Detection

A step is considered **successful** when both conditions are met:

1. `copilot.exe` exits with **exit code 0**.
2. The **expected output artifact file exists** at its defined path in the repository.

If either condition is not met, the step is treated as failed (see §6.3).

### 6.3 Failure Handling

| Failure Scenario | Orchestrator Behaviour |
|---|---|
| Non-zero exit code | Log error, pause, prompt human: retry / abort. |
| Zero exit code but artifact missing | Log warning, pause, prompt human: retry / abort. |
| Iteration cap reached | Log warning, pause, prompt human: override (mark pass) / abort. |
| Human rejects at phase gate | Log rejection with remarks, re-enter phase loop from work step. |

### 6.4 Prompt Construction

Each step has a **prompt template** — a plain text file stored in the repository at a well-known location:

```
<repo-root>/.orchestrator/prompts/<phase>/<step>.txt
```

The orchestrator constructs the final prompt by:

1. Loading the step's prompt template.
2. Substituting variables: `{{feature_description}}`, `{{input_artifact_path}}`, `{{review_remarks}}` (empty on first iteration).
3. On subsequent iterations, appending the remarks from the previous review artifact.

---

## 7. Artifact Conventions

### 7.1 Artifact Locations

Each step produces a single output artifact at a **predefined path** in the repository. These paths are defined in the orchestrator configuration file (see §8). The artifact type depends on the step:

| Step | Artifact Type |
|---|---|
| High Level Design | Markdown (`.md`) |
| Review HLD | JSON (verdict + remarks) |
| Detailed Design | Markdown (`.md`) |
| Review Detailed Design | JSON (verdict + remarks) |
| Code Implementation | C# source files (directory) |
| Unit Testing | C# test files (directory) |
| Code Review | JSON (verdict + remarks) |
| Integration Testing | C# test files + results (directory) |
| Review Integration Testing | JSON (verdict + remarks) |
| UAT | Markdown test report (`.md`) |
| Review UAT | JSON (verdict + remarks) |

### 7.2 Immutability After Phase Approval

Once a phase is approved by the human, its artifact paths are recorded in the state file as locked. The orchestrator will not invoke any agent targeting those paths again.

---

## 8. Configuration

### 8.1 Config File Location

```
<repo-root>/.orchestrator/config.json
```

### 8.2 Config Schema

```json
{
  "maxIterationsPerPhase": {
    "Design": 3,
    "Implementation": 3,
    "Test": 3
  },
  "remarkSeverityThreshold": {
    "Design": "minor",
    "Implementation": "minor",
    "Test": "minor"
  },
  "copilotExecutablePath": "copilot.exe",
  "phases": [
    {
      "name": "Design",
      "steps": [
        {
          "id": "hld",
          "name": "High Level Design",
          "type": "work",
          "agentName": "architect",
          "promptTemplate": ".orchestrator/prompts/design/hld.txt",
          "outputArtifact": "docs/design/hld.md"
        },
        {
          "id": "hld-review",
          "name": "Review High Level Design",
          "type": "review",
          "agentName": "reviewer",
          "promptTemplate": ".orchestrator/prompts/design/hld-review.txt",
          "outputArtifact": ".orchestrator/reviews/design/hld-review.json",
          "reviewTargetStepId": "hld"
        }
      ]
    }
  ]
}
```

### 8.3 Configurable Thresholds

`remarkSeverityThreshold` defines the highest severity level that is still considered a passing review. For example, a threshold of `"minor"` means a review passes if it contains only `minor` remarks (or none). A review with any `major` or `critical` remark triggers another iteration.

---

## 9. Human Approval Gate

### 9.1 Phase Gate Prompt

When all steps in a phase complete successfully, the orchestrator prints a summary to the console and prompts:

```
============================================================
PHASE COMPLETE: Design
============================================================
Artifacts produced:
  - docs/design/hld.md
  - docs/design/detailed-design.md

Iterations used: 2 / 3

Do you approve this phase and proceed to Implementation?
  [Y] Approve
  [N] Reject (you will be prompted for remarks)
  [A] Abort workflow

> 
```

### 9.2 Rejection Flow

If the human enters `N`, the orchestrator prompts for free-text remarks:

```
Enter rejection remarks (press Enter twice when done):
> 
```

These remarks are stored in the state file and injected into the next iteration of the phase's first work step prompt, alongside any agent-generated review remarks.

### 9.3 Abort Flow

If the human enters `A`, the orchestrator writes the current state to disk and exits cleanly. The workflow can be resumed from the same point later.

---

## 10. Console Output and Logging

### 10.1 Structured Console Output

The orchestrator writes structured, human-readable progress to stdout at all times:

```
[Orchestrator] Starting workflow: "Add user notification feature"
[Orchestrator] Resuming from state file — last completed: Design > Review HLD (iteration 1)

[Phase: Design] [Step: Detailed Design] [Iteration: 1/3]
[Phase: Design] Invoking agent: architect
[Phase: Design] Waiting for copilot.exe to complete...
[Phase: Design] Agent exited with code 0
[Phase: Design] Artifact found: docs/design/detailed-design.md ✓

[Phase: Design] [Step: Review Detailed Design] [Iteration: 1/3]
[Phase: Design] Invoking agent: reviewer
[Phase: Design] Agent exited with code 0
[Phase: Design] Review verdict: PASS (2 minor remarks)
[Phase: Design] Loop termination condition met — proceeding to phase gate
```

### 10.2 Log File

All console output is simultaneously written to:

```
<repo-root>/.orchestrator/logs/run-<timestamp>.log
```

---

## 11. Feature Input

### 11.1 Startup Input

On first run (no existing state file), the orchestrator prompts the user for a feature description at the terminal:

```
============================================================
WORKFLOW ORCHESTRATOR
============================================================
No existing workflow state found.

Enter a description of the feature to implement:
> 
```

This description is stored in the state file and used as `{{feature_description}}` in all prompt templates throughout the workflow.

### 11.2 Resume Input

On resume, the feature description is read from the state file. The user is not prompted again.

---

## 12. Non-Functional Requirements

| Requirement | Target |
|---|---|
| **Resumability** | Workflow must be resumable from last completed checkpoint after any unplanned interruption. |
| **Idempotency** | Re-running the orchestrator against an existing state must not re-execute approved steps. |
| **Observability** | Every agent invocation, verdict, and human decision must be recorded in the log file. |
| **Portability** | Runs on Windows (primary). No dependency on a CI/CD runner for v1. |
| **Extensibility** | Phase loop termination strategy must be abstracted behind an interface to allow non-deterministic strategies in future versions. |
| **Fail-safe** | Any unhandled exception must write the current state to disk before exiting. |

---

## 13. Repository Structure

```
<repo-root>/
├── .orchestrator/
│   ├── config.json              # Phase/step configuration and thresholds
│   ├── state.json               # Runtime state (auto-managed by orchestrator)
│   ├── prompts/
│   │   ├── design/
│   │   │   ├── hld.txt
│   │   │   ├── hld-review.txt
│   │   │   ├── detailed-design.txt
│   │   │   └── detailed-design-review.txt
│   │   ├── implementation/
│   │   │   ├── code-implementation.txt
│   │   │   ├── unit-testing.txt
│   │   │   └── code-review.txt
│   │   └── test/
│   │       ├── integration-testing.txt
│   │       ├── integration-review.txt
│   │       ├── uat.txt
│   │       └── uat-review.txt
│   ├── reviews/                 # Agent-generated review JSON artifacts
│   └── logs/                    # Timestamped run logs
├── docs/
│   └── design/                  # Design Markdown artifacts
└── src/                         # C# source and test artifacts
```

---

## 14. Future Considerations (Out of Scope for v1)

These items are explicitly deferred but should not be architecturally excluded:

- **Agent-driven approval**: Replace human phase gate with a supervisor agent that evaluates phase artifacts against acceptance criteria.
- **Non-deterministic loop termination**: Plug in an LLM-based evaluator that assesses remark trends across iterations to decide when diminishing returns justify stopping.
- **Parallel step execution**: Run independent review agents concurrently.
- **External system integration**: Post phase approvals to Jira; publish design artifacts to Confluence via MCP.
- **Configurable agent routing**: Select different Copilot agents (or models) per step based on step type and complexity budget.
