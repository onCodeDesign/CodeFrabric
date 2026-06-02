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
    private const int DefaultMaxIterations = 2;

    private static readonly SeverityThresholdPolicy s_loopPolicy = new(SeverityLevel.Minor);

    public string WorkflowType => "FeatureWorkflow";

    public IReadOnlyList<PhaseDefinition> Phases { get; } = BuildPhases();

    public LoopTerminationPolicy GetLoopPolicy(WorkReviewLoop loop) => s_loopPolicy;

    public string ResolveArtifactPath(WorkflowContext context, StepDefinition step)
    {
        string relativePath = step.OutputArtifactPath!
            .Replace("{#id}", context.IssueNumber, StringComparison.Ordinal)
            .Replace("{#iteration}", context.CurrentIteration.ToString(), StringComparison.Ordinal);
        return Path.Combine(context.RepositoryPath, ".cFabric", "orchestrator", relativePath);
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
                            PromptTemplatePath: "prompts/feature/design/01_high-level-design.md",
                            ReviewTargetStepId: null),
                new StepDefinition(
                            Id: "ReviewHighLevelDesign",
                            Name: "Review High Level Design",
                            Kind: StepKind.Improve,
                            AgentName: "architect",
                            PromptTemplatePath: "prompts/feature/design/02_review-high-level-design.md",
                            ReviewTargetStepId: "HighLevelDesign"),
                new StepDefinition(
                            Id: "DetailedDesign",
                            Name: "Detailed Design",
                            Kind: StepKind.Work,
                            AgentName: "detailed-designer",
                            PromptTemplatePath: "prompts/feature/design/03_detailed-design.md",
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
                            PromptTemplatePath: "prompts/feature/design/04_review-detailed-design.md",
                            ReviewTargetStepId: "DetailedDesign"),
                        new StepDefinition(
                              Id: "ImproveDetailedDesign",
                              Name: "Improve Detailed Design",
                              Kind: StepKind.ProcessReview,
                              AgentName: "detailed-designer",
                              PromptTemplatePath: "prompts/feature/design/05_improve-detailed-design.md",
                              ReviewTargetStepId: "ReviewDetailedDesign"),
                    ],
                    MaxIterations: DefaultMaxIterations),
            ]),

        new PhaseDefinition(
            Name: "Implement",
            Steps:
            [
               new StepDefinition(
                            Id: "CodeImplementation",
                            Name: "Code Implementation",
                            Kind: StepKind.Work,
                            AgentName: "coder",
                            PromptTemplatePath: "prompts/feature/implement/01_code-implementation.md",
                            ReviewTargetStepId: null),

                        new StepDefinition(
                            Id: "UnitTesting",
                            Name: "Unit Testing",
                            Kind: StepKind.Work,
                            AgentName: "SoftwareEngineer",
                            PromptTemplatePath: "prompts/feature/implement/02_unit-testing.md",
                            ReviewTargetStepId: null),

                new WorkReviewLoop(
                     Id: "ImplementationReviewLoop",
                    Name: "Implementation Review Loop",
                    Steps:
                    [
                       new StepDefinition(
                            Id: "CodeReview",
                            Name: "Code Review",
                            Kind: StepKind.Review,
                            AgentName: "code-reviewer",
                            PromptTemplatePath: "prompts/feature/implement/03_code-review.md",
                            ReviewTargetStepId: "CodeImplementation"),
                       new StepDefinition(
                            Id: "ProcessReview",
                            Name: "Process Code Review",
                            Kind: StepKind.ProcessReview,
                            AgentName: "coder",
                            PromptTemplatePath: "prompts/feature/implement/04_apply-code-review.md",
                            ReviewTargetStepId: "CodeReview"),
                    ],
                    MaxIterations: DefaultMaxIterations),
            ])
    ];
}
