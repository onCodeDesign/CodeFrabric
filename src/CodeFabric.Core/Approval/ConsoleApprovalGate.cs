using AppBoot.DependencyInjection;
using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Approval;

/// <summary>
/// Console-based implementation of <see cref="IApprovalGate"/>.
/// </summary>
[Service(typeof(IApprovalGate), ServiceLifetime.Transient)]
public sealed class ConsoleApprovalGate() : IApprovalGate
{
    /// <inheritdoc />
    public async Task<ApprovalDecision> RequestApprovalAsync(
        PhaseDefinition phase,
        IReadOnlyList<StepHistoryEntry> phaseHistory,
        CancellationToken ct)
    {
        Console.WriteLine($"═══ Phase Approval Gate: {phase.Name} ═══");

        foreach (StepHistoryEntry entry in phaseHistory)
        {
            Console.WriteLine($"  [{entry.Outcome}] {entry.StepId} (completed {entry.CompletedAt:HH:mm:ss})");
        }

        Console.WriteLine();

        while (true)
        {
            Console.Write($"Approve phase '{phase.Name}'? [Y]es / [N]o (reject with remarks) / [A]bort: ");
            string normalizedInput = (await ReadLineAsync(ct).ConfigureAwait(false))?.Trim() ?? string.Empty;

            if (normalizedInput.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || normalizedInput.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                return new ApprovalDecision(ApprovalOutcome.Approve, null);
            }

            if (normalizedInput.Equals("N", StringComparison.OrdinalIgnoreCase)
                || normalizedInput.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Enter rejection remarks: ");
                string? remarks = (await ReadLineAsync(ct).ConfigureAwait(false))?.Trim();
                return new ApprovalDecision(ApprovalOutcome.Reject, remarks);
            }

            if (normalizedInput.Equals("A", StringComparison.OrdinalIgnoreCase)
                || normalizedInput.Equals("Abort", StringComparison.OrdinalIgnoreCase))
            {
                return new ApprovalDecision(ApprovalOutcome.Abort, null);
            }

            Console.WriteLine("  Invalid input — please enter Y, N, or A.");
        }
    }

    private static async Task<string?> ReadLineAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }

        return await Task.Run(() => Console.ReadLine(), ct).ConfigureAwait(false);
    }
}
