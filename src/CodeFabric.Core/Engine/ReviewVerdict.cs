namespace CodeFabric.Core.Engine;

/// <summary>Machine-readable verdict produced by a Review step's HANDOUT block.</summary>
public sealed record ReviewVerdict(
    /// <summary>"pass" or "fail".</summary>
    string Outcome,
    IReadOnlyList<ReviewRemark> Remarks);

/// <summary>A single observation from a review step.</summary>
public sealed record ReviewRemark(
    /// <summary>Severity level: "critical", "major", or "minor".</summary>
    string Severity,
    string Message);
