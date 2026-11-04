using System.Diagnostics;
using System.Text;
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
    private const string ModelName = "claude-sonnet-5.5";
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
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
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

        Console.WriteLine($"▶ copilot --agent={invocation.AgentName}");

        using CancellationTokenRegistration registration = ct.Register(static state => TryKillProcess((Process)state!), process);

        Task<string> standardOutputTask = ForwardStreamAsync(process.StandardOutput, Console.Out);
        Task<string> standardErrorTask = ForwardStreamAsync(process.StandardError, Console.Error);

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

    /// <summary>
    /// Reads all lines from <paramref name="reader"/>, forwarding each to <paramref name="output"/>
    /// in real time, and returns the accumulated text.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="CancellationToken.None"/> for stream I/O so the task completes naturally
    /// when the process closes its pipe — even if the workflow is cancelled.
    /// </remarks>
    private static async Task<string> ForwardStreamAsync(StreamReader reader, TextWriter output)
    {
        var sb = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            sb.AppendLine(line);
            await output.WriteLineAsync(line).ConfigureAwait(false);
        }
        return sb.ToString();
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
