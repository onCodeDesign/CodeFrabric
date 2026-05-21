using AppBoot.DependencyInjection;
using CodeFabric.Core.Contracts;
using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Engine;

/// <summary>
/// Public entry point for starting and resuming workflow runs.
/// Resolves the correct <see cref="IWorkflowDefinition"/>, initialises or loads
/// a <see cref="WorkflowContext"/>, and delegates execution to <see cref="WorkflowEngine"/>.
/// </summary>
[Service(typeof(IWorkflowRunner), ServiceLifetime.Transient)]
public sealed class WorkflowRunner(
    IServiceProvider serviceProvider,
    IWorkflowStorage storage,
    IEnumerable<IWorkflowDefinition> definitions) : IWorkflowRunner
{
    /// <inheritdoc />
    public async Task<WorkflowRunResult> RunAsync(WorkflowRunRequest request, CancellationToken ct)
    {
        IWorkflowDefinition definition =
            definitions.FirstOrDefault(d => d.WorkflowType == request.WorkflowType)
            ?? throw new InvalidOperationException(
                $"Unknown workflow type: '{request.WorkflowType}'.");

        bool hasExisting = await storage.ExistsAsync(request.RepositoryPath, ct).ConfigureAwait(false);

        WorkflowContext context;

        if (request.Resume && hasExisting)
        {
            context = (await storage.LoadAsync(request.RepositoryPath, ct).ConfigureAwait(false))
                ?? throw new InvalidOperationException(
                    "Resume requested but the stored context could not be loaded.");
        }
        else
        {
            if (hasExisting && !request.Resume)
                throw new InvalidOperationException(
                    $"A workflow run already exists at '{request.RepositoryPath}'. " +
                    "Use --resume to continue it.");

            string workflowId =
                $"feature-{request.IssueNumber}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}";

            context = new WorkflowContext
            {
                WorkflowId     = workflowId,
                WorkflowType   = request.WorkflowType,
                RepositoryPath = request.RepositoryPath,
                IssueNumber    = request.IssueNumber,
                IssueShortName = request.IssueShortName,
                CurrentPhase   = definition.Phases[0].Name,
                CurrentStepId     = string.Empty,
                CurrentIteration  = 1,
                Status            = WorkflowStatus.NotStarted,
                StartedAt         = DateTimeOffset.UtcNow,
                CompletedAt       = null,
                History           = [],
                ApprovedPhases    = [],
            };

            await storage.SaveAsync(context, ct).ConfigureAwait(false);
        }

        IWorkflowEngine engine = serviceProvider.GetRequiredService<IWorkflowEngine>();
        WorkflowContext final = await engine.ExecuteAsync(context, definition, ct).ConfigureAwait(false);

        return new WorkflowRunResult(
            final.WorkflowId,
            final.Status,
            final.CurrentPhase,
            final.CurrentStepId,
            final.History);
    }
}
