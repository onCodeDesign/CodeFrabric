using System.Text.Json;
using System.Text.Json.Serialization;
using AppBoot.DependencyInjection;
using CodeFabric.Core.Agents;
using CodeFabric.Core.Approval;
using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.Git;
using CodeFabric.Core.Prompts;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Engine;

/// <summary>
/// Drives workflow execution for any <see cref="IWorkflowDefinition"/>.
/// The engine is generic — it has no knowledge of concrete phase names, agents, or ordering.
/// </summary>
[Service(typeof(IWorkflowEngine), ServiceLifetime.Transient)]
internal sealed class WorkflowEngine(
    IAgentRunner agentRunner,
    IApprovalGate approvalGate,
    IGitWorkspace gitWorkspace,
    IPromptRenderer promptRenderer,
    IWorkflowStorage storage) : IWorkflowEngine
{
    /// <summary>Advances a workflow context to completion or until aborted.</summary>
    public async Task<WorkflowContext> ExecuteAsync(
        WorkflowContext context,
        IWorkflowDefinition definition,
        CancellationToken ct)
    {
        if (context.Status == WorkflowStatus.NotStarted)
        {
            await gitWorkspace.EnsureCleanWorkingTreeAsync(context.RepositoryPath, ct).ConfigureAwait(false);
            await gitWorkspace.CreateAndCheckoutBranchAsync(
                context.RepositoryPath, InstanceBranch(context), ct).ConfigureAwait(false);
            context = context with { Status = WorkflowStatus.Running };
            await storage.SaveAsync(context, ct).ConfigureAwait(false);
        }

        foreach (PhaseDefinition phase in definition.Phases)
        {
            if (context.ApprovedPhases.Contains(phase.Name))
                continue;

            context = await ExecutePhaseAsync(context, phase, definition, ct).ConfigureAwait(false);

            if (context.Status is WorkflowStatus.Aborted or WorkflowStatus.Failed)
                return context;
        }

        context = context with { Status = WorkflowStatus.Completed, CompletedAt = DateTimeOffset.UtcNow };
        await storage.SaveAsync(context, ct).ConfigureAwait(false);
        return context;
    }

    private async Task<WorkflowContext> ExecutePhaseAsync(
        WorkflowContext context,
        PhaseDefinition phase,
        IWorkflowDefinition definition,
        CancellationToken ct)
    {
        string phaseBranch = PhaseBranch(phase.Name, context);

        // On first entry create the phase branch; on resume (or re-entry) just check it out.
        bool phaseStarted = context.History.Any(h => h.Phase == phase.Name);
        if (!phaseStarted)
        {
            await gitWorkspace.CheckoutAsync(
                context.RepositoryPath, InstanceBranch(context), ct).ConfigureAwait(false);
            await gitWorkspace.CreateAndCheckoutBranchAsync(
                context.RepositoryPath, phaseBranch, ct).ConfigureAwait(false);
        }
        else
        {
            await gitWorkspace.CheckoutAsync(
                context.RepositoryPath, phaseBranch, ct).ConfigureAwait(false);
        }

        string? rejectionRemarks = null;

        while (true) // outer loop handles approval rejection re-entry
        {
            foreach (IPhaseStep phaseStep in phase.Steps)
            {
                switch (phaseStep)
                {
                    case StepDefinition oneShot:
                        (context, _) = await ExecuteStepAsync(
                            context, oneShot, phase.Name, iteration: 1,
                            rejectionRemarks, definition, ct).ConfigureAwait(false);
                        break;

                    case WorkReviewLoop loop:
                        context = await ExecuteLoopAsync(
                            context, loop, phase.Name, rejectionRemarks, definition, ct).ConfigureAwait(false);
                        break;
                }
            }

            // Phase steps complete — request human approval.
            IReadOnlyList<StepHistoryEntry> phaseHistory =
                context.History.Where(h => h.Phase == phase.Name).ToList();

            context = context with { Status = WorkflowStatus.AwaitingApproval };
            await storage.SaveAsync(context, ct).ConfigureAwait(false);

            ApprovalDecision decision = await approvalGate
                .RequestApprovalAsync(phase, phaseHistory, ct).ConfigureAwait(false);

            switch (decision.Outcome)
            {
                case ApprovalOutcome.Approve:
                    await gitWorkspace.CheckoutAsync(
                        context.RepositoryPath, InstanceBranch(context), ct).ConfigureAwait(false);
                    await gitWorkspace.MergeNoFfAsync(
                        context.RepositoryPath,
                        phaseBranch,
                        $"Merge {phase.Name} phase [#{context.IssueNumber}]",
                        ct).ConfigureAwait(false);
                    context = context with
                    {
                        Status = WorkflowStatus.Running,
                        ApprovedPhases = [.. context.ApprovedPhases, phase.Name],
                    };
                    await storage.SaveAsync(context, ct).ConfigureAwait(false);
                    return context;

                case ApprovalOutcome.Reject:
                    rejectionRemarks = decision.Remarks;
                    context = context with { Status = WorkflowStatus.Running };
                    continue; // Re-enter phase with remarks

                default: // Abort
                    context = context with
                    {
                        Status = WorkflowStatus.Aborted,
                        CompletedAt = DateTimeOffset.UtcNow,
                    };
                    await storage.SaveAsync(context, ct).ConfigureAwait(false);
                    return context;
            }
        }
    }

    private async Task<WorkflowContext> ExecuteLoopAsync(
        WorkflowContext context,
        WorkReviewLoop loop,
        string phaseName,
        string? rejectionRemarks,
        IWorkflowDefinition definition,
        CancellationToken ct)
    {
        LoopTerminationPolicy policy = definition.GetLoopPolicy(loop);
        ReviewVerdict? verdict = null;

        for (int iter = 1; iter <= loop.MaxIterations; iter++)
        {
            foreach (StepDefinition step in loop.Steps)
            {
                ReviewVerdict? stepVerdict;
                (context, stepVerdict) = await ExecuteStepAsync(
                    context, step, phaseName, iter, rejectionRemarks, definition, ct).ConfigureAwait(false);

                if (stepVerdict is not null)
                    verdict = stepVerdict;
            }

            if (verdict is not null && policy.ShouldTerminate(verdict, iter, loop.MaxIterations))
                break;
        }

        return context;
    }

    private async Task<(WorkflowContext Context, ReviewVerdict? Verdict)> ExecuteStepAsync(
        WorkflowContext context,
        StepDefinition step,
        string phaseName,
        int iteration,
        string? rejectionRemarks,
        IWorkflowDefinition definition,
        CancellationToken ct)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        context = context with
        {
            CurrentPhase = phaseName,
            CurrentStepId = step.Id,
            CurrentIteration = iteration,
            Status = WorkflowStatus.Running,
        };

        string? artifactPath = step.OutputArtifactPath is not null
            ? definition.ResolveArtifactPath(context, step)
            : null;

        string templatePath = Path.Combine(context.RepositoryPath, step.PromptTemplatePath);

        IReadOnlyDictionary<string, string> variables = step.Kind switch
        {
            StepKind.Work =>
                BuildWorkVariables(context, iteration, rejectionRemarks),
            StepKind.Review =>
                BuildReviewVariables(context, phaseName, step),
            StepKind.Improve =>
                await BuildImproveVariablesAsync(context, phaseName, step, definition, ct).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unknown step kind: {step.Kind}"),
        };

        string renderedPrompt = await promptRenderer
            .RenderAsync(templatePath, variables, ct).ConfigureAwait(false);

        AgentInvocationResult result = await agentRunner.InvokeAsync(
            new AgentInvocation(step.AgentName, renderedPrompt, context.RepositoryPath),
            ct).ConfigureAwait(false);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"Agent '{step.AgentName}' (step '{step.Id}') exited {result.ExitCode}.\n{result.StdErr}");

        ReviewVerdict? verdict = null;
        if (step.Kind == StepKind.Review && result.HandoutBlock is not null)
            verdict = JsonSerializer.Deserialize(
                result.HandoutBlock, ReviewVerdictJsonContext.Default.ReviewVerdict);

        await gitWorkspace.StageAllAndCommitAsync(
            context.RepositoryPath,
            $"[{phaseName}] {step.Name} (iter {iteration})",
            ct).ConfigureAwait(false);

        StepHistoryEntry entry = new(
            phaseName, step.Id, iteration, StepOutcome.Succeeded,
            artifactPath, startedAt, DateTimeOffset.UtcNow, null);

        context = context with { History = [.. context.History, entry] };
        await storage.SaveAsync(context, ct).ConfigureAwait(false);

        return (context, verdict);
    }

    // ── Variable builders ────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, string> BuildWorkVariables(
        WorkflowContext context, int iteration, string? rejectionRemarks) =>
        new Dictionary<string, string>
        {
            ["issueNumber"]      = context.IssueNumber,
            ["phaseName"]        = context.CurrentPhase,
            ["iteration"]        = iteration.ToString(),
            ["rejectionRemarks"] = rejectionRemarks ?? string.Empty,
        };

    private static IReadOnlyDictionary<string, string> BuildReviewVariables(
        WorkflowContext context, string phaseName, StepDefinition step)
    {
        string workArtifactPath = ResolveLinkedArtifact(context, step.ReviewTargetStepId!, phaseName);
        return new Dictionary<string, string>
        {
            ["issueNumber"]      = context.IssueNumber,
            ["phaseName"]        = phaseName,
            ["workArtifactPath"] = workArtifactPath,
        };
    }

    private async Task<IReadOnlyDictionary<string, string>> BuildImproveVariablesAsync(
        WorkflowContext context,
        string phaseName,
        StepDefinition step,
        IWorkflowDefinition definition,
        CancellationToken ct)
    {
        // step.ReviewTargetStepId → the Review step whose artifact holds the remarks
        string reviewArtifactPath = ResolveLinkedArtifact(context, step.ReviewTargetStepId!, phaseName);
        string reviewContent = await File.ReadAllTextAsync(reviewArtifactPath, ct).ConfigureAwait(false);

        // The review step's ReviewTargetStepId → the Work artifact being improved
        StepDefinition reviewStep = FindStepById(definition, step.ReviewTargetStepId!);
        string workArtifactPath = ResolveLinkedArtifact(context, reviewStep.ReviewTargetStepId!, phaseName);

        return new Dictionary<string, string>
        {
            ["issueNumber"]           = context.IssueNumber,
            ["phaseName"]             = phaseName,
            ["reviewArtifactPath"]    = reviewArtifactPath,
            ["reviewArtifactContent"] = reviewContent,
            ["workArtifactPath"]      = workArtifactPath,
            ["remarks"]               = reviewContent,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ResolveLinkedArtifact(
        WorkflowContext context, string targetStepId, string phaseName)
    {
        StepHistoryEntry? entry = context.History
            .LastOrDefault(h => h.Phase == phaseName && h.StepId == targetStepId);

        if (entry?.ArtifactPath is null)
            throw new InvalidOperationException(
                $"No artifact found for step '{targetStepId}' in phase '{phaseName}'.");

        return entry.ArtifactPath;
    }

    private static StepDefinition FindStepById(IWorkflowDefinition definition, string stepId)
    {
        foreach (PhaseDefinition phase in definition.Phases)
            foreach (IPhaseStep phaseStep in phase.Steps)
            {
                if (phaseStep is StepDefinition sd && sd.Id == stepId) return sd;
                if (phaseStep is WorkReviewLoop loop)
                    foreach (StepDefinition ls in loop.Steps)
                        if (ls.Id == stepId) return ls;
            }

        throw new InvalidOperationException($"Step '{stepId}' not found in workflow definition.");
    }

    private static string InstanceBranch(WorkflowContext ctx) =>
        $"feature/#{ctx.IssueNumber}-{ctx.IssueShortName}";

    private static string PhaseBranch(string phaseName, WorkflowContext ctx) =>
        $"feature//#{ctx.IssueNumber}-{ctx.IssueShortName}//{phaseName.ToLowerInvariant()}";
}

[JsonSerializable(typeof(ReviewVerdict))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ReviewVerdictJsonContext : JsonSerializerContext;
