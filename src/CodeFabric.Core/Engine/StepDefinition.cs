namespace CodeFabric.Core.Engine;

/// <summary>
/// Defines a single executable step within a phase: the VS Code agent to invoke
/// and the prompt template to render for it.
/// </summary>
public sealed record StepDefinition(
    string Name,
    string AgentName,
    string PromptTemplatePath);
