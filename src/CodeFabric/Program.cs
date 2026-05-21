using AppBoot;
using AppBoot.AssemblyLoad;
using AppBoot.DependencyInjection;
using CodeFabric.Core.Contracts;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ── Cancellation ──────────────────────────────────────────────────────────────
using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// ── Argument parsing ──────────────────────────────────────────────────────────
// Usage:
//   codefabric run   --issue <n> [--repo <path>] [--workflow <type>]
//   codefabric resume            [--repo <path>] [--workflow <type>]

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
string? issueShortName = GetFlag(cliArgs, "--short-name");
string repoPath     = GetFlag(cliArgs, "--repo") ?? @"../";
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

if (command == "run" && string.IsNullOrWhiteSpace(issueShortName))
{
    Console.Error.WriteLine("--short-name <slug> is required for the 'run' command.");
    return 2;
}

WorkflowRunRequest request = new(
    WorkflowType:   workflowType,
    RepositoryPath: Path.GetFullPath(repoPath),
    IssueNumber:    issueNumber ?? string.Empty,
    IssueShortName: issueShortName ?? string.Empty,
    Resume:         command == "resume");

// ── Bootstrap ─────────────────────────────────────────────────────────────────
IBootstrapper bootstrapper = null!;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        bootstrapper = services
            .AddAppBoot(options =>
            {
                options.NameFilter = assembly => assembly.StartsWith("CodeFabric");
               options.PluginPathBuilderOption = PluginPathBuilderOption.SameRootBinDebug;
               options.AddPlugin("CodeFabric.Storage");

            })
            .AddRegistrationBehavior(new ServiceRegistrationBehavior())
            .Run();
    })
    .Build()
    .InitHostedApp(bootstrapper);

// ── Execute ───────────────────────────────────────────────────────────────────
IWorkflowRunner runner =  host.Services.GetRequiredService<IWorkflowRunner>();

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
          codefabric run    --issue <n> --short-name <slug> --repo <path> [--workflow <type>]
          codefabric resume             --repo <path> [--workflow <type>]

        Options:
          --issue <n>        GitHub issue number (required for 'run')
          --short-name <s>   Slug for branch names, e.g. "add-user-auth" (required for 'run')
          --repo  <path>     Path to the local repository
          --workflow <type>  Workflow type (default: FeatureWorkflow)
        """);
}
