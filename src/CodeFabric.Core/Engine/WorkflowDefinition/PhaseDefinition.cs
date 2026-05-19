namespace CodeFabric.Core.Engine.WorkflowDefinition;

/// <summary>
/// Describes a single workflow phase as an ordered sequence of
/// <see cref="IPhaseStep"/> elements — each element is either a one-shot
/// <see cref="StepDefinition"/> or a repeating <see cref="WorkReviewLoop"/>.
/// </summary>
public sealed record PhaseDefinition(
    string Name,
    IReadOnlyList<IPhaseStep> Steps);
