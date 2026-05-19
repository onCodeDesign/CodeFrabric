using CodeFabric.Core.State;

namespace CodeFabric.Core.Contracts;

/// <summary>Outcome returned to the CLI after a run completes or is interrupted.</summary>
public sealed record WorkflowRunResult(
    /// <summary>Unique identifier for the workflow run.</summary>
    string WorkflowId,
    /// <summary>Overall status of the workflow run.</summary>
    WorkflowStatus Status,
    /// <summary>Name of the phase currently in progress, if any.</summary>
    string? CurrentPhase,
    /// <summary>Identifier of the current step, if any.</summary>
    string? CurrentStep,
    /// <summary>Ordered history of completed workflow steps.</summary>
    IReadOnlyList<StepHistoryEntry> History);
