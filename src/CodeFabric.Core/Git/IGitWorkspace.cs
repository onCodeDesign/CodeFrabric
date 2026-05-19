namespace CodeFabric.Core.Git;

/// <summary>
/// Abstracts git operations needed by the workflow engine for branch management,
/// commits, and merges. The engine never invokes git directly.
/// </summary>
public interface IGitWorkspace
{
    /// <summary>
    /// Verifies the working tree is clean.
    /// Throws <see cref="InvalidOperationException"/> if uncommitted changes exist.
    /// </summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task EnsureCleanWorkingTreeAsync(string repositoryPath, CancellationToken ct);

    /// <summary>Creates a new branch from HEAD and checks it out.</summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="branchName">Name of the branch to create and checkout.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task CreateAndCheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken ct);

    /// <summary>Checks out an existing branch.</summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="branchName">Name of the branch to checkout.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task CheckoutAsync(string repositoryPath, string branchName, CancellationToken ct);

    /// <summary>Stages all changes (git add -A) and creates a commit.</summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="message">Commit message.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task StageAllAndCommitAsync(string repositoryPath, string message, CancellationToken ct);

    /// <summary>
    /// Merges <paramref name="sourceBranch"/> into the current branch using --no-ff
    /// so the merge commit is always preserved.
    /// </summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="sourceBranch">Branch to merge into the current branch.</param>
    /// <param name="commitMessage">Merge commit message.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    Task MergeNoFfAsync(string repositoryPath, string sourceBranch, string commitMessage, CancellationToken ct);

    /// <summary>Returns the full SHA of the HEAD commit on the current branch.</summary>
    /// <param name="repositoryPath">Absolute path to the git repository.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    /// <returns>The full SHA for the current HEAD commit.</returns>
    Task<string> GetHeadShaAsync(string repositoryPath, CancellationToken ct);
}
