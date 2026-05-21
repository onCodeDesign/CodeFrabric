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

        WorkflowContext context;

        if (request.Mode == WorkflowRunMode.Resume)
        {
            if (string.IsNullOrWhiteSpace(request.InstanceId))
                throw new InvalidOperationException(
                    "Resume requires an InstanceId identifying the workflow instance to resume.");

            context = (await storage
                    .LoadAsync(request.RepositoryPath, request.InstanceId, ct)
                    .ConfigureAwait(false))
                ?? throw new InvalidOperationException(
                    $"No workflow instance '{request.InstanceId}' found for issue '{request.IssueNumber}'.");
        }
        else
        {
            IReadOnlyList<string> existing = await storage
                .ListInstancesAsync(request.RepositoryPath, request.IssueNumber, ct)
                .ConfigureAwait(false);

            if (existing.Count > 0 && request.Mode == WorkflowRunMode.Start)
                throw new InvalidOperationException(
                    $"Workflow instance(s) already exist for issue '{request.IssueNumber}': " +
                    $"[{string.Join(", ", existing)}]. " +
                    "Pass --resume <instanceId> to continue one, or --new to start an additional instance.");

            string workflowId =
                $"feature-{request.IssueNumber}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";

            context = new WorkflowContext
            {
                WorkflowId       = workflowId,
                WorkflowType     = request.WorkflowType,
                RepositoryPath   = request.RepositoryPath,
                IssueNumber      = request.IssueNumber,
                IssueShortName   = request.IssueShortName,
                CurrentPhase     = definition.Phases[0].Name,
                CurrentStepId    = string.Empty,
                CurrentIteration = 1,
                Status           = WorkflowStatus.NotStarted,
                StartedAt        = DateTimeOffset.UtcNow,
                CompletedAt      = null,
                History          = [],
                ApprovedPhases   = [],
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
