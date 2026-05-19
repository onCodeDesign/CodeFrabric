namespace CodeFabric.Core.Agents;

/// <summary>
/// Parameters for a single agent invocation.
/// </summary>
public sealed record AgentInvocation(
    /// <summary>The VS Code agent persona name, e.g. "architect".</summary>
    string AgentName,
    /// <summary>The fully rendered prompt to pass to the agent.</summary>
    string RenderedPrompt,
    /// <summary>The working directory for the agent process.</summary>
    string WorkingDirectory);
