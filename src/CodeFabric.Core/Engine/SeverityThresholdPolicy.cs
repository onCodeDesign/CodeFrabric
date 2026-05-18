namespace CodeFabric.Core.Engine;

/// <summary>
/// Terminates the phase loop when the review verdict is "pass", all remarks are at or
/// below <paramref name="threshold"/> severity, or the maximum number of iterations is reached.
/// </summary>
public sealed class SeverityThresholdPolicy(SeverityLevel threshold) : LoopTerminationPolicy
{
    public override bool ShouldTerminate(ReviewVerdict verdict, int currentIteration, int maxIterations)
    {
        if (currentIteration >= maxIterations) return true;
        if (string.Equals(verdict.Outcome, "pass", StringComparison.OrdinalIgnoreCase)) return true;
        return verdict.Remarks.All(r => ParseSeverity(r.Severity) <= threshold);
    }

    private static SeverityLevel ParseSeverity(string severity) =>
        severity.ToLowerInvariant() switch
        {
            "critical" => SeverityLevel.Critical,
            "major"    => SeverityLevel.Major,
            _          => SeverityLevel.Minor,
        };
}
