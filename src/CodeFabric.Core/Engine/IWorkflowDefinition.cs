using CodeFabric.Core.State;

namespace CodeFabric.Core.Engine;

/// <summary>
/// Encapsulates everything specific to a workflow type: phases, steps,
/// loop-termination policies, and artifact-path resolution.
/// The engine is generic and consumes this contract without any knowledge
/// of concrete phase names, agent names, or ordering.
/// </summary>
public interface IWorkflowDefinition
{
    /// <summary>The unique type identifier for this workflow, e.g. "FeatureWorkflow".</summary>
    string WorkflowType { get; }

    /// <summary>Ordered list of phases that make up this workflow.</summary>
    IReadOnlyList<PhaseDefinition> Phases { get; }

    /// <summary>
    /// Returns the <see cref="LoopTerminationPolicy"/> that governs the
    /// work-review loop for the given phase.
    /// </summary>
    LoopTerminationPolicy GetLoopPolicy(PhaseDefinition phase);

    /// <summary>
    /// Resolves the full output artifact path for a step given the current
    /// runtime context (e.g. repository path, iteration number).
    /// </summary>
    string ResolveArtifactPath(WorkflowContext context, StepDefinition step);
}
