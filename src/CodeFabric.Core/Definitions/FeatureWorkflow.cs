using AppBoot.DependencyInjection;
using CodeFabric.Core.Engine;
using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Definitions;

/// <summary>
/// Workflow definition for the standard feature delivery lifecycle:
/// Design → Implementation → Test.
/// </summary>
[Service(typeof(IWorkflowDefinition), ServiceLifetime.Singleton)]
public sealed class FeatureWorkflow : IWorkflowDefinition
{
    private const int DefaultMaxIterations = 3;

    private static readonly SeverityThresholdPolicy s_loopPolicy = new(SeverityLevel.Minor);

    public string WorkflowType => "FeatureWorkflow";

    public IReadOnlyList<PhaseDefinition> Phases { get; } = BuildPhases();

    public LoopTerminationPolicy GetLoopPolicy(WorkReviewLoop loop) => s_loopPolicy;

    public string ResolveArtifactPath(WorkflowContext context, StepDefinition step)
    {
        string relativePath = step.OutputArtifactPath!
            .Replace("{#id}", context.IssueNumber, StringComparison.Ordinal)
            .Replace("{#iteration}", context.CurrentIteration.ToString(), StringComparison.Ordinal);
        return Path.Combine(context.RepositoryPath, ".orchestrator", relativePath);
    }

    private static IReadOnlyList<PhaseDefinition> BuildPhases() =>
    [
        new PhaseDefinition(
            Name: "Design",
            Steps:
            [
               new StepDefinition(
                            Id: "HighLevelDesign",
                            Name: "High Level Design",
                            Kind: StepKind.Work,
                            AgentName: "architect",
                            PromptTemplatePath: "prompts/feature/design/high-level-design.md",
                            OutputArtifactPath: "docs/workitems/{#id}-design.md",
                            ReviewTargetStepId: null),
                new WorkReviewLoop(
                   Id: "HighLevelDesignReviewLoop",
                   Name: "High Level Design",
                    Steps:
                    [
                        new StepDefinition(
                            Id: "ReviewHighLevelDesign",
                            Name: "Review High Level Design",
                            Kind: StepKind.Review,
                            AgentName: "architect",
                            PromptTemplatePath: "prompts/feature/design/review-high-level-design.md",
                            OutputArtifactPath: "workingSpace/workitems/{#id}-hld-review.md",
                            ReviewTargetStepId: "HighLevelDesign"),
                       new StepDefinition(
                          Id: "ImproveHighLevelDesign",
                            Name: "Improve High Level Design",
                            Kind: StepKind.Improve,
                            AgentName: "architect",
                            PromptTemplatePath: "prompts/feature/design/improve-high-level-design.md",
                            OutputArtifactPath: "docs/workitems/{#id}-design.md",
                            ReviewTargetStepId: "ReviewHighLevelDesign"),
                    ],
                    MaxIterations: DefaultMaxIterations),

                new StepDefinition(
                            Id: "DetailedDesign",
                            Name: "Detailed Design",
                            Kind: StepKind.Work,
                            AgentName: "detailed-designer",
                            PromptTemplatePath: "prompts/feature/design/detailed-design.md",
                            OutputArtifactPath: "docs/workitems/{#id}-detailed-design.md",
                            ReviewTargetStepId: null),

                new WorkReviewLoop(
                     Id: "DetailedDesignReviewLoop",
                    Name: "Review Detailed Design",
                    Steps:
                    [
                        new StepDefinition(
                            Id: "ReviewDetailedDesign",
                            Name: "Review Detailed Design",
                            Kind: StepKind.Review,
                            AgentName: "detailed-designer",
                            PromptTemplatePath: "prompts/feature/design/review-detailed-design.md",
                            OutputArtifactPath: "workingSpace/workitems/{#id}/dd-review-{#iteration}.md",
                            ReviewTargetStepId: "DetailedDesign"),
                        new StepDefinition(
                              Id: "ImproveDetailedDesign",
                              Name: "Improve Detailed Design",
                              Kind: StepKind.Improve,
                              AgentName: "detailed-designer",
                              PromptTemplatePath: "prompts/feature/design/improve-detailed-design.md",
                              OutputArtifactPath: "docs/workitems/{#id}-detailed-design.md",
                              ReviewTargetStepId: "ReviewDetailedDesign"),
                    ],
                    MaxIterations: DefaultMaxIterations),
            ]),

        new PhaseDefinition(
            Name: "Implementation",
            Steps:
            [
               new StepDefinition(
                            Id: "CodeImplementation",
                            Name: "Code Implementation",
                            Kind: StepKind.Work,
                            AgentName: "coder",
                            PromptTemplatePath: "prompts/feature/implementation/code-implementation.md",
                            OutputArtifactPath: null,
                            ReviewTargetStepId: null),

                        new StepDefinition(
                            Id: "UnitTesting",
                            Name: "Unit Testing",
                            Kind: StepKind.Work,
                            AgentName: "SoftwareEngineer",
                            PromptTemplatePath: "prompts/feature/implementation/unit-testing.md",
                            OutputArtifactPath: null,
                            ReviewTargetStepId: null),

                new WorkReviewLoop(
                     Id: "ImplementationLoop",
                    Name: "Implementation",
                    Steps:
                    [
                       new StepDefinition(
                            Id: "CodeReview",
                            Name: "Code Review",
                            Kind: StepKind.Review,
                            AgentName: "code-reviewer",
                            PromptTemplatePath: "prompts/feature/implementation/code-review.md",
                            OutputArtifactPath: "workingSpace/workitems/{#id}/implementation/code-review-{#iteration}.json",
                            ReviewTargetStepId: "CodeImplementation"),
                       new StepDefinition(
                            Id: "ImproveCode",
                            Name: "Improve Code",
                            Kind: StepKind.Improve,
                            AgentName: "coder",
                            PromptTemplatePath: "prompts/feature/implementation/improve-code.md",
                            OutputArtifactPath: null,
                            ReviewTargetStepId: "CodeReview"),
                    ],
                    MaxIterations: DefaultMaxIterations),
            ])
    ];
}
