namespace CodeFabric.Core.Approval;

/// <summary>The decision returned by an <see cref="IApprovalGate"/>.</summary>
/// <param name="Outcome">Outcome selected by the reviewer.</param>
/// <param name="Remarks">Human-provided remarks; populated only when <paramref name="Outcome"/> is <see cref="ApprovalOutcome.Reject"/>.</param>
public sealed record ApprovalDecision(
    ApprovalOutcome Outcome,
    string? Remarks);
