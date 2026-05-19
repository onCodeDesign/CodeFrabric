namespace CodeFabric.Core.State;

/// <summary>
/// Abstraction for persisting and loading <see cref="WorkflowContext"/>.
/// The engine never touches file paths or storage formats directly.
/// </summary>
public interface IWorkflowStorage
{
    /// <summary>
    /// Loads the persisted context for the given repository, or returns
    /// <see langword="null"/> if no run exists yet.
    /// </summary>
    /// <param name="repositoryPath">Absolute path to the repository whose workflow context should be loaded.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    /// <returns>The persisted <see cref="WorkflowContext"/>, or <see langword="null"/> if none exists.</returns>
    Task<WorkflowContext?> LoadAsync(string repositoryPath, CancellationToken ct);

    /// <summary>Persists the current context, overwriting any previous state.</summary>
    /// <param name="context">Workflow context to persist.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task SaveAsync(WorkflowContext context, CancellationToken ct);

    /// <summary>Returns <see langword="true"/> if a persisted context exists for the repository.</summary>
    /// <param name="repositoryPath">Absolute path to the repository whose persisted workflow context should be checked.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    /// <returns><see langword="true"/> if persisted context exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(string repositoryPath, CancellationToken ct);
}
