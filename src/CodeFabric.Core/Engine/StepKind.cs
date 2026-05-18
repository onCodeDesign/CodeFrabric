namespace CodeFabric.Core.Engine;

/// <summary>Discriminates between steps that produce work and steps that review prior work.</summary>
public enum StepKind
{
    /// <summary>Produces a new artifact (design document, code, test report, etc.).</summary>
    Work,

    /// <summary>Evaluates a prior artifact and returns a structured review verdict.</summary>
    Review,
}
