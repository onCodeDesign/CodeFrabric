namespace CodeFabric.Core.Contracts;

/// <summary>Specifies how a workflow run should be initiated.</summary>
public enum WorkflowRunMode
{
    /// <summary>Start a brand-new workflow instance. Fails if any instance already exists for the same issue.</summary>
    Start,

    /// <summary>Resume an existing workflow instance identified by <see cref="WorkflowRunRequest.InstanceId"/>.</summary>
    Resume,

    /// <summary>Start an additional workflow instance for an issue that already has one or more existing instances.</summary>
    New,
}
