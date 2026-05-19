namespace CodeFabric.Core.Contracts;

/// <summary>
/// Entry point for starting and resuming workflow runs.
/// The CLI interacts with Core exclusively through this interface.
/// </summary>
public interface IWorkflowRunner
{
    /// <summary>Starts a new workflow run or resumes an existing one.</summary>
    /// <param name="request">Describes the workflow run to execute.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The final or interrupted workflow run result.</returns>
    Task<WorkflowRunResult> RunAsync(WorkflowRunRequest request, CancellationToken ct);
}
