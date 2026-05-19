namespace CodeFabric.Core.Agents;

/// <summary>
/// Raw output from a single agent invocation.
/// </summary>
public sealed record AgentInvocationResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    /// <summary>The HANDOUT block parsed from stdout, or null if not present.</summary>
    string? HandoutBlock);
