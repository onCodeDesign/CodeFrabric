namespace CodeFabric.Core.Engine;

/// <summary>
/// Ordered severity levels used by <see cref="LoopTerminationPolicy"/> implementations.
/// Lower numeric values are less severe.
/// </summary>
public enum SeverityLevel
{
    Minor = 0,
    Major = 1,
    Critical = 2,
}
