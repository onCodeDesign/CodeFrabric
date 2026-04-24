# CodeFabric — Detailed Design

**Version:** 0.1
**Status:** Draft
**Companion document:** [high-level-design.md](./high-level-design.md)

This document holds the concrete type definitions and contracts referenced from the
High-Level Design. It is expected to evolve as implementation proceeds.

---

## 1. Public contracts (`CodeFabric.Core.Contracts`)

The CLI interacts with Core exclusively through these types.

```csharp
public interface IWorkflowRunner
{
    Task<WorkflowRunResult> RunAsync(WorkflowRunRequest request, CancellationToken ct);
}

public sealed record WorkflowRunRequest(
    string WorkflowType,          // e.g. "FeatureWorkflow"
    string RepositoryPath,
    string? FeatureDescription,   // null on resume
    bool Resume);

public sealed record WorkflowRunResult(
    string WorkflowId,
    WorkflowStatus Status,
    string? CurrentPhase,
    string? CurrentStep,
    IReadOnlyList<StepHistoryEntry> History);
```

---

## 2. Workflow definition (`CodeFabric.Core.Engine`)

```csharp
public interface IWorkflowDefinition
{
    string WorkflowType { get; }
    IReadOnlyList<PhaseDefinition> Phases { get; }
    LoopTerminationPolicy GetLoopPolicy(PhaseDefinition phase);
    string ResolveArtifactPath(WorkflowContext context, StepDefinition step);
}

public sealed record PhaseDefinition(
    string Name,
    IReadOnlyList<StepDefinition> Steps,
    int MaxIterations);

public sealed record StepDefinition(
    string Id,
    string Name,
    StepKind Kind,                // Work | Review
    string AgentName,
    string PromptTemplatePath,
    string OutputArtifactPath,
    string? ReviewTargetStepId);  // set only for Review steps

public enum StepKind { Work, Review }
```

`LoopTerminationPolicy` is a strategy object returned per phase. It receives the latest
review verdict and decides whether the phase loop should continue or stop.

---

## 3. Agent invocation (`CodeFabric.Core.Agents`)

```csharp
public interface IAgentRunner
{
    Task<AgentInvocationResult> InvokeAsync(AgentInvocation invocation, CancellationToken ct);
}

public sealed record AgentInvocation(
    string AgentName,
    string RenderedPrompt,
    string WorkingDirectory);

public sealed record AgentInvocationResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    string? HandoutBlock);
```

The concrete `CopilotCliAgentRunner` spawns `copilot.exe`, writes `RenderedPrompt` to
stdin, captures stdout/stderr, and parses the HANDOUT block. Success is decided by the
engine, not the runner (`exit == 0` AND artifact exists AND HANDOUT present).

---

## 4. Storage contract (`CodeFabric.Core.State`)

```csharp
public interface IWorkflowStorage
{
    Task<WorkflowContext?> LoadAsync(string repositoryPath, CancellationToken ct);
    Task SaveAsync(WorkflowContext context, CancellationToken ct);
    Task<bool> ExistsAsync(string repositoryPath, CancellationToken ct);
}
```

`JsonWorkflowStorage` (in `CodeFabric.Storage`) persists to
`<repositoryPath>/.orchestrator/state.json` via `System.Text.Json` using atomic
write-to-temp + rename.

---

## 5. Approval gate (`CodeFabric.Core.Approval`)

```csharp
public interface IApprovalGate
{
    Task<ApprovalDecision> RequestApprovalAsync(
        PhaseDefinition phase,
        IReadOnlyList<StepHistoryEntry> phaseHistory,
        CancellationToken ct);
}

public sealed record ApprovalDecision(
    ApprovalOutcome Outcome,
    string? Remarks);

public enum ApprovalOutcome { Approve, Reject, Abort }
```

`ConsoleApprovalGate` is the v1 implementation (Y/N/A console prompt per
[REQUIREMENTS.md §9](./REQUIREMENTS.md)).

---

## 6. Workflow state aggregate

```csharp
public sealed record WorkflowContext
{
    public required string WorkflowId { get; init; }
    public required string WorkflowType { get; init; }
    public required string RepositoryPath { get; init; }
    public required string FeatureDescription { get; init; }
    public required string CurrentPhase { get; init; }
    public required string CurrentStepId { get; init; }
    public required int CurrentIteration { get; init; }
    public required WorkflowStatus Status { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required IReadOnlyList<StepHistoryEntry> History { get; init; }
    public required IReadOnlyList<string> ApprovedPhases { get; init; }
}

public sealed record StepHistoryEntry(
    string Phase,
    string StepId,
    int Iteration,
    StepOutcome Outcome,
    string? ArtifactPath,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? FailureReason);

public enum WorkflowStatus { NotStarted, Running, AwaitingApproval, Completed, Aborted, Failed }
public enum StepOutcome { Succeeded, Failed, Skipped }
```

`WorkflowContext` is persisted by the engine after every state transition.

---

## 7. Open items

- Configuration schema (`IWorkflowConfiguration`) — to be specified.
- Prompt rendering contract (`IPromptRenderer`) — to be specified.
- Error taxonomy and CLI exit-code mapping.
- Logging/telemetry schema.
