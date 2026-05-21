namespace CodeFabric.Core.Contracts;

/// <summary>Describes a new or resumed workflow run.</summary>
public sealed record WorkflowRunRequest(
    /// <summary>Identifies the IWorkflowDefinition to use, e.g. "FeatureWorkflow".</summary>
    string WorkflowType,
    /// <summary>Absolute path to the repository being orchestrated.</summary>
    string RepositoryPath,
    /// <summary>GitHub issue number. The engine fetches description + title from GitHub.</summary>
    string IssueNumber,
    /// <summary>Short, slug-friendly name for the issue used in branch names, e.g. "add-user-auth".</summary>
    string IssueShortName,
    /// <summary>How the run should be initiated: fresh start, resume, or new-alongside-existing.</summary>
    WorkflowRunMode Mode,
    /// <summary>Required when <see cref="Mode"/> is <see cref="WorkflowRunMode.Resume"/>; ignored otherwise.</summary>
    string? InstanceId = null);
