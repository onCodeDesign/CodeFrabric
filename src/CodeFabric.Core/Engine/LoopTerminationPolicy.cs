namespace CodeFabric.Core.Engine;

/// <summary>
/// Strategy that decides whether a phase's work-review loop should terminate.
/// Returned per-phase by <see cref="IWorkflowDefinition.GetLoopPolicy"/>.
/// </summary>
public abstract class LoopTerminationPolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when the loop should stop and the phase
    /// should be considered complete (pending approval).
    /// </summary>
    /// <param name="verdict">The verdict from the most recent review step.</param>
    /// <param name="currentIteration">1-based index of the iteration just completed.</param>
    /// <param name="maxIterations">Maximum iterations configured for the phase.</param>
    public abstract bool ShouldTerminate(ReviewVerdict verdict, int currentIteration, int maxIterations);
}
