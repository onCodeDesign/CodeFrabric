namespace CodeFabric.Core.Agents;

/// <summary>
/// Abstracts the invocation of a named agent persona with a rendered prompt.
/// </summary>
public interface IAgentRunner
{
    /// <summary>
    /// Invokes the configured agent with the supplied prompt.
    /// </summary>
    /// <param name="invocation">The invocation parameters.</param>
    /// <param name="ct">The cancellation token for the running process.</param>
    /// <returns>The raw invocation result.</returns>
    Task<AgentInvocationResult> InvokeAsync(AgentInvocation invocation, CancellationToken ct);
}
