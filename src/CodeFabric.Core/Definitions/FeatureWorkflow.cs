using CodeFabric.Core.Engine;
using CodeFabric.Core.State;

namespace CodeFabric.Core.Definitions;

/// <summary>
/// Workflow definition for the standard feature delivery lifecycle:
/// Design → Implementation → Test.
/// </summary>
internal sealed class FeatureWorkflow : IWorkflowDefinition
{
    private const int DefaultMaxIterations = 3;

    private static readonly SeverityThresholdPolicy s_loopPolicy = new(SeverityLevel.Minor);

    public string WorkflowType => "FeatureWorkflow";

    public IReadOnlyList<PhaseDefinition> Phases { get; } = BuildPhases();

    public LoopTerminationPolicy GetLoopPolicy(PhaseDefinition phase) => s_loopPolicy;

    public string ResolveArtifactPath(WorkflowContext context, StepDefinition step) =>
        Path.Combine(context.RepositoryPath, ".orchestrator", step.OutputArtifactPath);

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
                    AgentName: "c",
                    PromptTemplatePath: "prompts/feature/design/high-level-design.md",
                    OutputArtifactPath: "docs/workitems/{#id}-design.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "ReviewHighLevelDesign",
                    Name: "Review High Level Design",
                    Kind: StepKind.Review,
                    AgentName: "architect",
                    PromptTemplatePath: "prompts/feature/design/review-high-level-design.md",
                    OutputArtifactPath: "workingSpace/workitems/{#id}-hld-review.md",
                    ReviewTargetStepId: "HighLevelDesign"),

                new StepDefinition(
                    Id: "DetailedDesign",
                    Name: "Detailed Design",
                    Kind: StepKind.Work,
                    AgentName: "detailed-designer",
                    PromptTemplatePath: "prompts/feature/design/detailed-design.md",
                    OutputArtifactPath: "docs/workitems/{#id}-detailed-design.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "ReviewDetailedDesign",
                    Name: "Review Detailed Design",
                    Kind: StepKind.Review,
                    AgentName: "detailed-designer",
                    PromptTemplatePath: "prompts/feature/design/review-detailed-design.md",
                    OutputArtifactPath: "workingSpace/workitems/{#id}-dd-review.md",
                    ReviewTargetStepId: "DetailedDesign"),
            ],
            MaxIterations: DefaultMaxIterations),

        new PhaseDefinition(
            Name: "Implementation",
            Steps:
            [
                new StepDefinition(
                    Id: "CodeImplementation",
                    Name: "Code Implementation",
                    Kind: StepKind.Work,
                    AgentName: "SoftwareEngineer",
                    PromptTemplatePath: "prompts/feature/implementation/code-implementation.md",
                    OutputArtifactPath: "outputs/implementation/code-summary.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "UnitTesting",
                    Name: "Unit Testing",
                    Kind: StepKind.Work,
                    AgentName: "SoftwareEngineer",
                    PromptTemplatePath: "prompts/feature/implementation/unit-testing.md",
                    OutputArtifactPath: "outputs/implementation/unit-tests-summary.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "CodeReview",
                    Name: "Code Review",
                    Kind: StepKind.Review,
                    AgentName: "CodeReviewer",
                    PromptTemplatePath: "prompts/feature/implementation/code-review.md",
                    OutputArtifactPath: "outputs/implementation/code-review.json",
                    ReviewTargetStepId: "CodeImplementation"),
            ],
            MaxIterations: DefaultMaxIterations),

        new PhaseDefinition(
            Name: "Test",
            Steps:
            [
                new StepDefinition(
                    Id: "IntegrationTesting",
                    Name: "Integration Testing",
                    Kind: StepKind.Work,
                    AgentName: "QAEngineer",
                    PromptTemplatePath: "prompts/feature/test/integration-testing.md",
                    OutputArtifactPath: "outputs/test/integration-tests-summary.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "ReviewIntegrationTesting",
                    Name: "Review Integration Testing",
                    Kind: StepKind.Review,
                    AgentName: "TestReviewer",
                    PromptTemplatePath: "prompts/feature/test/review-integration-testing.md",
                    OutputArtifactPath: "outputs/test/integration-tests-review.json",
                    ReviewTargetStepId: "IntegrationTesting"),

                new StepDefinition(
                    Id: "UserAcceptanceTesting",
                    Name: "User Acceptance Testing",
                    Kind: StepKind.Work,
                    AgentName: "QAEngineer",
                    PromptTemplatePath: "prompts/feature/test/uat.md",
                    OutputArtifactPath: "outputs/test/uat-summary.md",
                    ReviewTargetStepId: null),

                new StepDefinition(
                    Id: "ReviewUAT",
                    Name: "Review UAT",
                    Kind: StepKind.Review,
                    AgentName: "TestReviewer",
                    PromptTemplatePath: "prompts/feature/test/review-uat.md",
                    OutputArtifactPath: "outputs/test/uat-review.json",
                    ReviewTargetStepId: "UserAcceptanceTesting"),
            ],
            MaxIterations: DefaultMaxIterations),
    ];
}

