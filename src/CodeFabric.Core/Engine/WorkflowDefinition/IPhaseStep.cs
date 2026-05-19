namespace CodeFabric.Core.Engine.WorkflowDefinition;

/// <summary>
/// Base type for elements that can appear in a <see cref="PhaseDefinition"/>'s
/// step sequence. Concrete subtypes are <see cref="StepDefinition"/> (a single
/// one-shot step) and <see cref="WorkReviewLoop"/> (an iterated group of steps).
/// </summary>
public interface IPhaseStep
{
   string Id { get; }
   string Name { get; }
}