using System.Text.Json;
using System.Text.Json.Serialization;
using AppBoot.DependencyInjection;
using CodeFabric.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFabric.Storage;

/// <summary>
/// Persists <see cref="WorkflowContext"/> as indented JSON under
/// <c>{repositoryPath}/.workflow/context.json</c>.
/// Writes atomically via a temporary file followed by rename to prevent
/// corrupt state on crash or cancellation.
/// </summary>
[Service(typeof(IWorkflowStorage), ServiceLifetime.Singleton)]
public sealed class JsonWorkflowStorage : IWorkflowStorage
{
    private static string IssueDirectory(string repositoryPath, string issueNumber) =>
        Path.Combine(repositoryPath, ".workflow", issueNumber);

    private static string InstanceFilePath(string repositoryPath, string issueNumber, string instanceId) =>
        Path.Combine(IssueDirectory(repositoryPath, issueNumber), instanceId + ".json");

    /// <inheritdoc />
    public async Task<WorkflowContext?> LoadAsync(
        string repositoryPath, string instanceId, CancellationToken ct)
    {
        // The instance lives under one of the issue subdirectories — locate the file by name.
        string root = Path.Combine(repositoryPath, ".workflow");
        if (!Directory.Exists(root))
            return null;

        string? match = Directory
            .EnumerateFiles(root, instanceId + ".json", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (match is null)
            return null;

        await using FileStream stream = new(
            match, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        return await JsonSerializer
            .DeserializeAsync(stream, WorkflowContextJsonContext.Default.WorkflowContext, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowContext context, CancellationToken ct)
    {
        string targetPath = InstanceFilePath(context.RepositoryPath, context.IssueNumber, context.WorkflowId);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        string tempPath = targetPath + ".tmp";

        await using (FileStream stream = new(
            tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            await JsonSerializer
                .SerializeAsync(stream, context, WorkflowContextJsonContext.Default.WorkflowContext, ct)
                .ConfigureAwait(false);
        }

        // Atomic rename — overwrites any previous file.
        File.Move(tempPath, targetPath, overwrite: true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListInstancesAsync(
        string repositoryPath, string issueNumber, CancellationToken ct)
    {
        string dir = IssueDirectory(repositoryPath, issueNumber);
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<string>>([]);

        IReadOnlyList<string> ids = Directory
            .EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToList();

        return Task.FromResult(ids);
    }
}

[JsonSerializable(typeof(WorkflowContext))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class WorkflowContextJsonContext : JsonSerializerContext;
