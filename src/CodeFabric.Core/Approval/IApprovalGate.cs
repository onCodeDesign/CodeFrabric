using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.State;

namespace CodeFabric.Core.Approval;

/// <summary>
/// Called by the engine at each phase boundary to obtain an approve, reject, or abort decision.
/// </summary>
public interface IApprovalGate
{
    /// <summary>Requests approval for a completed phase.</summary>
    /// <param name="phase">Phase awaiting human review.</param>
    /// <param name="phaseHistory">Completed step history entries for the phase.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    /// <returns>The human approval decision.</returns>
    Task<ApprovalDecision> RequestApprovalAsync(
        PhaseDefinition phase,
        IReadOnlyList<StepHistoryEntry> phaseHistory,
        CancellationToken ct);
}
