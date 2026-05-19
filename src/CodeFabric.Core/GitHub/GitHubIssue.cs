namespace CodeFabric.Core.GitHub;

/// <summary>A GitHub issue retrieved via the <c>gh</c> CLI.</summary>
public sealed record GitHubIssue(
    /// <summary>The numeric issue identifier.</summary>
    int Number,
    /// <summary>The issue title.</summary>
    string Title,
    /// <summary>The full issue body (Markdown).</summary>
    string Body);
