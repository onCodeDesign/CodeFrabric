using System.Text.Json;
using System.Text.Json.Serialization;
using CodeFabric.Core.Engine;
using CodeFabric.Core.Engine.WorkflowDefinition;

namespace CodeFabric.Core.Definitions;

/// <summary>
/// Discovers and loads <see cref="IWorkflowDefinition"/> instances from
/// <c>*.json</c> files in a given folder. Each file must conform to
/// <see cref="WorkflowFileModel"/>.
/// </summary>
public static class JsonWorkflowDefinitionLoader
{
    /// <summary>
    /// Returns one <see cref="IWorkflowDefinition"/> per <c>*.json</c> file found
    /// in <paramref name="folderPath"/>. Returns an empty list if the folder does
    /// not exist.
    /// </summary>
    public static IReadOnlyList<IWorkflowDefinition> LoadFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        return Directory
            .EnumerateFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LoadFromFile)
            .ToList();
    }

    private static IWorkflowDefinition LoadFromFile(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        WorkflowFileModel model =
            JsonSerializer.Deserialize(stream, WorkflowFileJsonContext.Default.WorkflowFileModel)
            ?? throw new InvalidOperationException(
                $"Failed to deserialise workflow definition file '{filePath}'.");

        return new JsonWorkflowDefinition(model);
    }
}

// ── Runtime implementation ────────────────────────────────────────────────────

internal sealed class JsonWorkflowDefinition(WorkflowFileModel model) : IWorkflowDefinition
{
    private readonly LoopTerminationPolicy _loopPolicy = BuildLoopPolicy(model.LoopPolicy);

    /// <inheritdoc />
    public string WorkflowType { get; } = model.WorkflowType;

    /// <inheritdoc />
    public IReadOnlyList<PhaseDefinition> Phases { get; } = BuildPhases(model.Phases);

    /// <inheritdoc />
    public LoopTerminationPolicy GetLoopPolicy(WorkReviewLoop loop) => _loopPolicy;

    // ── Builder helpers ───────────────────────────────────────────────────────

    private static LoopTerminationPolicy BuildLoopPolicy(LoopPolicyModel policy) =>
        policy.Kind switch
        {
            "SeverityThreshold" => new SeverityThresholdPolicy(
                Enum.Parse<SeverityLevel>(policy.Threshold ?? "Minor", ignoreCase: true)),
            _ => throw new InvalidOperationException(
                $"Unknown loop policy kind: '{policy.Kind}'.")
        };

    private static IReadOnlyList<PhaseDefinition> BuildPhases(IReadOnlyList<PhaseModel> phases) =>
        phases.Select(p => new PhaseDefinition(p.Name, BuildPhaseSteps(p.Steps))).ToList();

    private static IReadOnlyList<IPhaseStep> BuildPhaseSteps(IReadOnlyList<PhaseStepModel> steps) =>
        steps.Select(BuildPhaseStep).ToList();

    private static IPhaseStep BuildPhaseStep(PhaseStepModel step) => step switch
    {
        StepModel s => ToStepDefinition(s),
        LoopModel l => new WorkReviewLoop(
            l.Id,
            l.Name,
            l.Steps.Select(ToStepDefinition).ToList(),
            l.MaxIterations),
        _ => throw new InvalidOperationException(
            $"Unknown phase-step model type '{step.GetType().Name}'.")
    };

    private static StepDefinition ToStepDefinition(StepModel s) => new(
        s.Id,
        s.Name,
        Enum.Parse<StepKind>(s.Kind, ignoreCase: true),
        s.AgentName,
        s.PromptTemplatePath,
        s.ReviewTargetStepId);
}

// ── JSON model records ────────────────────────────────────────────────────────

/// <summary>Top-level model for a workflow definition JSON file.</summary>
public sealed record WorkflowFileModel(
    string WorkflowType,
    LoopPolicyModel LoopPolicy,
    IReadOnlyList<PhaseModel> Phases);

/// <summary>Configures the loop-termination strategy for all work-review loops.</summary>
public sealed record LoopPolicyModel(string Kind, string? Threshold);

/// <summary>A single named phase containing an ordered list of steps.</summary>
public sealed record PhaseModel(string Name, IReadOnlyList<PhaseStepModel> Steps);

/// <summary>
/// Discriminated union base for phase-step models.
/// Set <c>"$type": "step"</c> for a one-shot step or <c>"$type": "loop"</c> for a loop.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StepModel), "step")]
[JsonDerivedType(typeof(LoopModel), "loop")]
public abstract record PhaseStepModel(string Id, string Name);

/// <summary>A single executable step within a phase.</summary>
public sealed record StepModel(
    string Id,
    string Name,
    string Kind,
    string AgentName,
    string PromptTemplatePath,
    string? ReviewTargetStepId) : PhaseStepModel(Id, Name);

/// <summary>A group of steps that repeat as an iterative work-review cycle.</summary>
public sealed record LoopModel(
    string Id,
    string Name,
    int MaxIterations,
    IReadOnlyList<StepModel> Steps) : PhaseStepModel(Id, Name);

// ── JSON source-generation context ───────────────────────────────────────────

[JsonSerializable(typeof(WorkflowFileModel))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
internal sealed partial class WorkflowFileJsonContext : JsonSerializerContext;
