namespace CodeFabric.Core.Engine.WorkflowDefinition;

/// <summary>
/// Describes a single executable step within a phase.
/// </summary>
public sealed record StepDefinition(
    /// <summary>Unique identifier within the workflow, e.g. "HighLevelDesign".</summary>
    string Id,
    /// <summary>Human-readable display name.</summary>
    string Name,
    /// <summary>Whether this step produces work output or reviews prior output.</summary>
    StepKind Kind,
    /// <summary>Name of the VS Code agent persona to invoke.</summary>
    string AgentName,
    /// <summary>Workspace-relative path to the Markdown prompt template.</summary>
    string PromptTemplatePath,
    /// <summary>Path relative to the repository root where the output artifact is written.</summary>
    string? OutputArtifactPath,
    /// <summary>
    /// For <see cref="StepKind.Review"/> steps, the <see cref="StepDefinition.Id"/>
    /// of the work step whose artifact is being reviewed. <see langword="null"/> for work steps.
    /// </summary>
    string? ReviewTargetStepId) : IPhaseStep;
