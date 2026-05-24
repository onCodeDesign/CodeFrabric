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
//   codefabric --issue <n> --short-name <slug> [--repo <path>] [--workflow <type>]
//              [ --resume <instanceId> | --new ]

string[] cliArgs = Environment.GetCommandLineArgs()[1..];

if (cliArgs.Length == 0 || cliArgs[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

string? issueNumber    = GetFlag(cliArgs, "--issue");
string? issueShortName = GetFlag(cliArgs, "--short-name");
string  repoPath       = GetFlag(cliArgs, "--repo") ?? @"../";
string  workflowType   = GetFlag(cliArgs, "--workflow") ?? "FeatureWorkflow";
string? resumeId       = GetFlag(cliArgs, "--resume");
bool    newFlag        = HasFlag(cliArgs, "--new");

if (issueNumber is null)
{
    Console.Error.WriteLine("--issue <n> is required.");
    return 2;
}

if (resumeId is not null && newFlag)
{
    Console.Error.WriteLine("--resume and --new are mutually exclusive.");
    return 2;
}

WorkflowRunMode mode = resumeId is not null
    ? WorkflowRunMode.Resume
    : newFlag
        ? WorkflowRunMode.New
        : WorkflowRunMode.Start;

if (mode != WorkflowRunMode.Resume && string.IsNullOrWhiteSpace(issueShortName))
{
    Console.Error.WriteLine("--short-name <slug> is required when starting a new instance.");
    return 2;
}

WorkflowRunRequest request = new(
    WorkflowType:   workflowType,
    RepositoryPath: Path.GetFullPath(repoPath),
    IssueNumber:    issueNumber,
    IssueShortName: issueShortName ?? string.Empty,
    Mode:           mode,
    InstanceId:     resumeId);

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

    Console.WriteLine($"[CodeFabric] instance : {result.WorkflowId}  (short-id: {result.ShortId})");
    Console.WriteLine($"[CodeFabric] status   : {result.Status}");

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

static bool HasFlag(string[] args, string flag) => Array.IndexOf(args, flag) >= 0;

static void PrintUsage()
{
    Console.WriteLine("""
        CodeFabric — AI workflow orchestrator

        Usage:
          codefabric --issue <n> --short-name <slug> [--repo <path>] [--workflow <type>]
          codefabric --issue <n> --resume <instanceId> [--repo <path>] [--workflow <type>]
          codefabric --issue <n> --short-name <slug> --new [--repo <path>] [--workflow <type>]

        Options:
          --issue <n>          GitHub issue number (required)
          --short-name <s>     Slug for branch names, e.g. "add-user-auth" (required unless --resume)
          --resume <id>        Resume the workflow instance with the given id
          --new                Start an additional instance for an issue that already has one
          --repo <path>        Path to the local repository (default: ../)
          --workflow <type>    Workflow type (default: FeatureWorkflow)

        Behavior:
          Without --resume or --new, the tool fails if any workflow instance already
          exists for the given issue. Use --resume <id> to continue an existing one,
          or --new to start an additional instance alongside it.
        """);
}
