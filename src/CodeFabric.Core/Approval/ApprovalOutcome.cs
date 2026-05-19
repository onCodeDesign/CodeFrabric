namespace CodeFabric.Core.Approval;

/// <summary>The three possible outcomes from a human phase-gate review.</summary>
public enum ApprovalOutcome
{
    /// <summary>Phase is approved; proceed to the next phase.</summary>
    Approve,

    /// <summary>Phase is rejected; re-enter the phase loop with the provided remarks.</summary>
    Reject,

    /// <summary>The entire workflow run is aborted immediately.</summary>
    Abort,
}
