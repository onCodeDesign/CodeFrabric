namespace CodeFabric.Core.Engine.WorkflowDefinition;

/// <summary>
/// A group of steps within a phase that repeat together as an iterative work-review cycle.
/// The loop continues until the <see cref="IWorkflowDefinition.GetLoopPolicy"/> signals
/// termination or <see cref="MaxIterations"/> is reached.
/// </summary>
public sealed record WorkReviewLoop(
   string Id,
    string Name,
    IReadOnlyList<StepDefinition> Steps,
    int MaxIterations) : IPhaseStep;
