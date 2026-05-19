using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AppBoot.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.GitHub;

/// <summary>
/// Fetches GitHub issue data by shelling out to the <c>gh</c> CLI.
/// Requires <c>gh</c> to be installed and authenticated in the environment.
/// </summary>
[Service(typeof(IGitHubService), ServiceLifetime.Transient)]
public sealed partial class GitHubCliService : IGitHubService
{
    /// <inheritdoc />
    public async Task<GitHubIssue> FetchIssueAsync(
        string repositoryPath, string issueNumber, CancellationToken ct)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "gh",
            WorkingDirectory = repositoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("issue");
        startInfo.ArgumentList.Add("view");
        startInfo.ArgumentList.Add(issueNumber);
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("number,title,body");

        using Process process = new() { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException("Failed to start the gh CLI process.");

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"gh issue view {issueNumber} failed (exit {process.ExitCode}):\n{stderr.Trim()}");

        GhIssuePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize(stdout, GhIssuePayloadContext.Default.GhIssuePayload);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse gh issue output: {stdout}", ex);
        }

        if (payload is null)
            throw new InvalidOperationException($"gh returned null payload for issue {issueNumber}.");

        return new GitHubIssue(payload.number, payload.title, payload.body);
    }

    /// <summary>
    /// Converts an issue title into a short URL-safe slug suitable for branch names.
    /// Lowercases, replaces non-alphanumeric runs with hyphens, and truncates to 50 chars.
    /// </summary>
    public static string SlugifyTitle(string title)
    {
        string lower = title.ToLowerInvariant();
        string hyphenated = NonAlphanumericRunRegex().Replace(lower, "-");
        string trimmed = hyphenated.Trim('-');
        return trimmed.Length <= 50 ? trimmed : trimmed[..50].TrimEnd('-');
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphanumericRunRegex();

    // Private DTO for JSON deserialization — field names match gh CLI output exactly.
    private sealed record GhIssuePayload(int number, string title, string body);

    [JsonSerializable(typeof(GhIssuePayload))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    private sealed partial class GhIssuePayloadContext : JsonSerializerContext;
}
