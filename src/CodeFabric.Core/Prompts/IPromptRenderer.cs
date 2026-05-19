namespace CodeFabric.Core.Prompts;

/// <summary>
/// Renders agent prompts by loading a Markdown template file and substituting
/// <c>{{variableName}}</c> placeholders with runtime values.
/// </summary>
/// <remarks>
/// Well-known variable names supplied by the engine:
/// <list type="bullet">
///   <item><c>{{featureDescription}}</c> — the GitHub issue body.</item>
///   <item><c>{{issueNumber}}</c> — the GitHub issue number.</item>
///   <item><c>{{phaseName}}</c> — the name of the current phase.</item>
///   <item><c>{{iteration}}</c> — the 1-based iteration counter within the current loop.</item>
///   <item><c>{{rejectionRemarks}}</c> — remarks from a human rejection at the phase gate.</item>
///   <item><c>{{reviewArtifactPath}}</c> — path to the review artifact (Improve steps).</item>
///   <item><c>{{reviewArtifactContent}}</c> — full text of the review artifact (Improve steps).</item>
///   <item><c>{{workArtifactPath}}</c> — path to the work artifact being improved (Improve steps).</item>
///   <item><c>{{remarks}}</c> — structured remarks from the review verdict (Improve steps).</item>
/// </list>
/// </remarks>
public interface IPromptRenderer
{
    /// <summary>
    /// Loads the template at <paramref name="templatePath"/> and replaces every
    /// <c>{{key}}</c> placeholder with the corresponding value from
    /// <paramref name="variables"/>. Keys not present in the template are silently ignored.
    /// </summary>
    /// <param name="templatePath">Absolute path to the Markdown template file.</param>
    /// <param name="variables">Map of placeholder name to substitution value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="templatePath"/> does not exist.
    /// </exception>
    Task<string> RenderAsync(
        string templatePath,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct);
}
