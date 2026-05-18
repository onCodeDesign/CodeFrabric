namespace CodeFabric.Core.State;

/// <summary>Overall status of a workflow run.</summary>
public enum WorkflowStatus
{
    NotStarted,
    Running,
    AwaitingApproval,
    Completed,
    Aborted,
    Failed,
}
