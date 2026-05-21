using System.Diagnostics;
using AppBoot.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Core.Git;

/// <summary>
/// Implements <see cref="IGitWorkspace"/> by invoking the git command-line client.
/// </summary>
[Service(typeof(IGitWorkspace), ServiceLifetime.Transient)]
public sealed class GitCliWorkspace : IGitWorkspace
{
    /// <inheritdoc />
    public async Task EnsureCleanWorkingTreeAsync(string repositoryPath, CancellationToken ct)
    {
        string output = await RunGitAsync(repositoryPath, ["status", "--porcelain"], ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("Working tree has uncommitted changes. Please commit or stash before running CodeFabric. We recommend using git worktree for isolated Agentic workflows.");
        }
    }

    /// <inheritdoc />
    public Task CreateAndCheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken ct)
        => RunGitAsync(repositoryPath, ["checkout", "-b", branchName], ct);

    /// <inheritdoc />
    public Task CheckoutAsync(string repositoryPath, string branchName, CancellationToken ct)
        => RunGitAsync(repositoryPath, ["checkout", branchName], ct);

    /// <inheritdoc />
    public async Task StageAllAndCommitAsync(string repositoryPath, string message, CancellationToken ct)
    {
        await RunGitAsync(repositoryPath, ["add", "-A"], ct).ConfigureAwait(false);
        await RunGitAsync(repositoryPath, ["commit", "-m", message], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task MergeNoFfAsync(string repositoryPath, string sourceBranch, string commitMessage, CancellationToken ct)
        => RunGitAsync(repositoryPath, ["merge", "--no-ff", sourceBranch, "-m", commitMessage], ct);

    /// <inheritdoc />
    public Task<string> GetHeadShaAsync(string repositoryPath, CancellationToken ct)
        => RunGitAsync(repositoryPath, ["rev-parse", "HEAD"], ct);

    private static async Task<string> RunGitAsync(string repositoryPath, string[] args, CancellationToken ct)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "git",
            WorkingDirectory = repositoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = new()
        {
            StartInfo = startInfo,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the git process.");
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

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(" ", args)} failed (exit {process.ExitCode}):\n{standardError.Trim()}");
        }

        return standardOutput.Trim();
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
}
