namespace CodeFabric.Core.Engine;

/// <summary>
/// Identifies the current position within a workflow execution.
/// Used internally by the engine for navigation and resumability.
/// </summary>
internal sealed record WorkflowStep(
    PhaseDefinition Phase,
    StepDefinition Step,
    int Iteration);