namespace CodeFabric.Core.GitHub;

/// <summary>Fetches GitHub issue data needed to initialise a workflow run.</summary>
public interface IGitHubService
{
    /// <summary>
    /// Returns the issue identified by <paramref name="issueNumber"/> from the
    /// GitHub repository associated with <paramref name="repositoryPath"/>.
    /// </summary>
    /// <param name="repositoryPath">
    /// Absolute path to the local repository. Used as the working directory when
    /// invoking the <c>gh</c> CLI so it picks up the correct remote automatically.
    /// </param>
    /// <param name="issueNumber">The GitHub issue number, e.g. "42".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>gh</c> CLI fails or the response cannot be parsed.
    /// </exception>
    Task<GitHubIssue> FetchIssueAsync(string repositoryPath, string issueNumber, CancellationToken ct);
}
