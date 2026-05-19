namespace CodeFabric.Core.State;

/// <summary>
/// The single persisted aggregate that represents a workflow run.
/// Written after every state transition to enable resumability.
/// </summary>
public sealed record WorkflowContext
{
    /// <summary>Unique run identifier, e.g. "feature-42-20250324-1430".</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Identifies the <see cref="Engine.WorkflowDefinition.IWorkflowDefinition"/> to use, e.g. "FeatureWorkflow".</summary>
    public required string WorkflowType { get; init; }

    /// <summary>Absolute path to the repository being orchestrated.</summary>
    public required string RepositoryPath { get; init; }

    /// <summary>Human-provided description of the feature being built.</summary>
    public required string FeatureDescription { get; init; }

    /// <summary>Name of the phase currently executing.</summary>
    public required string CurrentPhase { get; init; }

    /// <summary>Id of the step currently executing within <see cref="CurrentPhase"/>.</summary>
    public required string CurrentStepId { get; init; }

    /// <summary>1-based iteration count within the current phase.</summary>
    public required int CurrentIteration { get; init; }

    public required WorkflowStatus Status { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Full ordered history of every completed step execution.</summary>
    public required IReadOnlyList<StepHistoryEntry> History { get; init; }

    /// <summary>Names of phases that have received human approval and must not be re-executed.</summary>
    public required IReadOnlyList<string> ApprovedPhases { get; init; }
}
