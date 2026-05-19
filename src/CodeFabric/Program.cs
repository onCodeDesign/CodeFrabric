using CodeFabric.Core.Agents;
using CodeFabric.Core.Approval;
using CodeFabric.Core.Contracts;
using CodeFabric.Core.Definitions;
using CodeFabric.Core.Engine;
using CodeFabric.Core.Engine.WorkflowDefinition;
using CodeFabric.Core.Git;
using CodeFabric.Core.GitHub;
using CodeFabric.Core.Prompts;
using CodeFabric.Core.State;
using CodeFabric.Storage;
using Microsoft.Extensions.DependencyInjection;

// ── Cancellation ──────────────────────────────────────────────────────────────
using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// ── DI container ─────────────────────────────────────────────────────────────
// TODO: Replace with AppBoot-bootstrapped registration once configured.
ServiceCollection services = new();
services.AddSingleton<IWorkflowStorage, JsonWorkflowStorage>();
services.AddSingleton<IWorkflowDefinition, FeatureWorkflow>();
services.AddTransient<IAgentRunner, CopilotCliAgentRunner>();
services.AddTransient<IApprovalGate, ConsoleApprovalGate>();
services.AddTransient<IGitWorkspace, GitCliWorkspace>();
services.AddTransient<IPromptRenderer, TemplatePromptRenderer>();
services.AddTransient<IGitHubService, GitHubCliService>();
services.AddTransient<IWorkflowRunner, WorkflowRunner>();

await using ServiceProvider sp = services.BuildServiceProvider();

// ── Argument parsing ──────────────────────────────────────────────────────────
// Usage:
//   codefabric run   --issue <n> --repo <path> [--workflow <type>]
//   codefabric resume            --repo <path> [--workflow <type>]

string[] cliArgs = Environment.GetCommandLineArgs()[1..];

if (cliArgs.Length == 0 || cliArgs[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

string command = cliArgs[0].ToLowerInvariant();
if (command is not ("run" or "resume"))
{
    Console.Error.WriteLine($"Unknown command '{cliArgs[0]}'. Expected 'run' or 'resume'.");
    PrintUsage();
    return 2;
}

string? issueNumber  = GetFlag(cliArgs, "--issue");
string? repoPath     = GetFlag(cliArgs, "--repo");
string  workflowType = GetFlag(cliArgs, "--workflow") ?? "FeatureWorkflow";

if (repoPath is null)
{
    Console.Error.WriteLine("--repo <path> is required.");
    return 2;
}

if (command == "run" && issueNumber is null)
{
    Console.Error.WriteLine("--issue <n> is required for the 'run' command.");
    return 2;
}

WorkflowRunRequest request = new(
    WorkflowType:   workflowType,
    RepositoryPath: Path.GetFullPath(repoPath),
    IssueNumber:    issueNumber ?? string.Empty,
    Resume:         command == "resume");

// ── Execute ───────────────────────────────────────────────────────────────────
IWorkflowRunner runner = sp.GetRequiredService<IWorkflowRunner>();

try
{
    WorkflowRunResult result = await runner.RunAsync(request, cts.Token);

    Console.WriteLine($"Workflow {result.WorkflowId} finished with status: {result.Status}");

    return result.Status switch
    {
        WorkflowStatus.Completed => 0,
        WorkflowStatus.Aborted   => 1,
        _                        => 3,
    };
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Workflow cancelled.");
    return 130; // SIGINT convention
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal: {ex.Message}");
    return 1;
}

// ── Helpers ───────────────────────────────────────────────────────────────────
static string? GetFlag(string[] args, string flag)
{
    int i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static void PrintUsage()
{
    Console.WriteLine("""
        CodeFabric — AI workflow orchestrator

        Usage:
          codefabric run    --issue <n> --repo <path> [--workflow <type>]
          codefabric resume             --repo <path> [--workflow <type>]

        Options:
          --issue <n>       GitHub issue number (required for 'run')
          --repo  <path>    Path to the local repository
          --workflow <type> Workflow type (default: FeatureWorkflow)
        """);
}
