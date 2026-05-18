namespace CodeFabric.Core.Engine;

/// <summary>
/// Describes a single workflow phase: its name, ordered steps, and the
/// maximum number of work-review iterations allowed before escalation.
/// </summary>
public sealed record PhaseDefinition(
    string Name,
    IReadOnlyList<StepDefinition> Steps,
    int MaxIterations);
