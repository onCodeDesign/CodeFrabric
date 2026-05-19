using System.Diagnostics;
using System.Text.RegularExpressions;
using AppBoot.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Agents;

/// <summary>
/// Runs agent personas through the Copilot CLI.
/// </summary>
[Service(typeof(IAgentRunner), ServiceLifetime.Transient)]
public sealed partial class CopilotCliAgentRunner() : IAgentRunner
{
    private const string CopilotExecutable = "copilot";
    private const string ModelName = "claude-sonnet-4.6";
    private const string HandoutContentGroupName = "content";

    /// <inheritdoc />
    public async Task<AgentInvocationResult> InvokeAsync(AgentInvocation invocation, CancellationToken ct)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = CopilotExecutable,
            WorkingDirectory = invocation.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add($"--agent={invocation.AgentName}");
        startInfo.ArgumentList.Add($"--model={ModelName}");
        startInfo.ArgumentList.Add("--allow-all-tools");
        startInfo.ArgumentList.Add("--autopilot");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(invocation.RenderedPrompt);

        using Process process = new()
        {
            StartInfo = startInfo,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the Copilot CLI agent process.");
        }

        using CancellationTokenRegistration registration = ct.Register(static state => TryKillProcess((Process)state!), process);

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(ct);

        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        string standardOutput = await standardOutputTask.ConfigureAwait(false);
        string standardError = await standardErrorTask.ConfigureAwait(false);

        return new AgentInvocationResult(
            process.ExitCode,
            standardOutput,
            standardError,
            ParseHandoutBlock(standardOutput));
    }

    private static string? ParseHandoutBlock(string standardOutput)
    {
        Match handoutCommentMatch = HandoutCommentRegex().Match(standardOutput);
        if (handoutCommentMatch.Success)
        {
            return handoutCommentMatch.Groups[HandoutContentGroupName].Value.Trim();
        }

        Match fencedHandoutMatch = FencedHandoutRegex().Match(standardOutput);
        if (fencedHandoutMatch.Success)
        {
            return fencedHandoutMatch.Groups[HandoutContentGroupName].Value.Trim();
        }

        return null;
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    [GeneratedRegex(@"<!--\s*HANDOUT(?<content>[\s\S]*?)-->", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HandoutCommentRegex();

    [GeneratedRegex(@"```handout\s*(?<content>[\s\S]*?)```", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FencedHandoutRegex();
}
