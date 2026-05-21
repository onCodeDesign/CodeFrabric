namespace CodeFabric.Core.State;

/// <summary>
/// Abstraction for persisting and loading <see cref="WorkflowContext"/>.
/// The engine never touches file paths or storage formats directly.
/// A workflow instance is uniquely identified by <see cref="WorkflowContext.IssueNumber"/>
/// combined with <see cref="WorkflowContext.WorkflowId"/>.
/// </summary>
public interface IWorkflowStorage
{
    /// <summary>Loads a specific persisted workflow instance, or returns <see langword="null"/> if not found.</summary>
    /// <param name="repositoryPath">Absolute path to the repository.</param>
    /// <param name="instanceId">The unique workflow instance id (<see cref="WorkflowContext.WorkflowId"/>).</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task<WorkflowContext?> LoadAsync(string repositoryPath, string instanceId, CancellationToken ct);

    /// <summary>Persists the current context, overwriting any previous state of the same instance.</summary>
    /// <param name="context">Workflow context to persist.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task SaveAsync(WorkflowContext context, CancellationToken ct);

    /// <summary>Returns the ids of all persisted workflow instances tied to the given issue.</summary>
    /// <param name="repositoryPath">Absolute path to the repository.</param>
    /// <param name="issueNumber">GitHub issue number to filter by.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task<IReadOnlyList<string>> ListInstancesAsync(
        string repositoryPath, string issueNumber, CancellationToken ct);
}
