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
    private static string ContextFilePath(string repositoryPath) =>
        Path.Combine(repositoryPath, ".workflow", "context.json");

    /// <inheritdoc />
    public async Task<WorkflowContext?> LoadAsync(string repositoryPath, CancellationToken ct)
    {
        string path = ContextFilePath(repositoryPath);
        if (!File.Exists(path))
            return null;

        await using FileStream stream = new(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        return await JsonSerializer
            .DeserializeAsync(stream, WorkflowContextJsonContext.Default.WorkflowContext, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowContext context, CancellationToken ct)
    {
        string targetPath = ContextFilePath(context.RepositoryPath);
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
    public Task<bool> ExistsAsync(string repositoryPath, CancellationToken ct) =>
        Task.FromResult(File.Exists(ContextFilePath(repositoryPath)));
}

[JsonSerializable(typeof(WorkflowContext))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class WorkflowContextJsonContext : JsonSerializerContext;
