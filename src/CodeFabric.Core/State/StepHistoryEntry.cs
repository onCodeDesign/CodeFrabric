namespace CodeFabric.Core.State;

/// <summary>Immutable record of a single completed step execution, written to the history.</summary>
public sealed record StepHistoryEntry(
    string Phase,
    string StepId,
    int Iteration,
    StepOutcome Outcome,
    string? ArtifactPath,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? FailureReason);
