using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.State;

namespace CodeFabric.Core.Engine;

/// <summary>
/// Drives execution of a workflow from its current <see cref="WorkflowContext"/>
/// to completion or until aborted.
/// </summary>
internal interface IWorkflowEngine
{
    /// <summary>Advances a workflow context to completion or until aborted.</summary>
    Task<WorkflowContext> ExecuteAsync(
        WorkflowContext context,
        IWorkflowDefinition definition,
        CancellationToken ct);
}
