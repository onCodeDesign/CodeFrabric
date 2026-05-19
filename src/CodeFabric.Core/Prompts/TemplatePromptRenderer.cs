using AppBoot.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Prompts;

/// <summary>
/// Loads a Markdown template file and substitutes <c>{{key}}</c> placeholders
/// with caller-supplied values.
/// </summary>
[Service(typeof(IPromptRenderer), ServiceLifetime.Transient)]
public sealed class TemplatePromptRenderer : IPromptRenderer
{
    /// <inheritdoc />
    public async Task<string> RenderAsync(
        string templatePath,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Prompt template not found: {templatePath}", templatePath);

        string template = await File.ReadAllTextAsync(templatePath, ct).ConfigureAwait(false);

        foreach (KeyValuePair<string, string> variable in variables)
            template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value, StringComparison.Ordinal);

        return template;
    }
}
