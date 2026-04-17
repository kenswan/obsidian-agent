using System.ComponentModel;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class NoteTools(
    VaultService vaultService,
    ILogger<NoteTools> logger)
{
    [McpServerTool, Description("Read the content of a note from the Obsidian vault")]
    public async Task<object> ReadNote(
        string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading note '{Path}'", path);

        string fullPath = vaultService.ResolveNotePath(path);

        if (!vaultService.IsInsideVault(fullPath))
        {
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        string content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);

        return new
        {
            Path = path,
            Content = content,
            LastModified = File.GetLastWriteTimeUtc(fullPath)
        };
    }

    [McpServerTool, Description("Create a new note in the Obsidian vault. Fails if the note already exists.")]
    public async Task<object> CreateNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}'", path);

        string fullPath = vaultService.ResolveNotePath(path);

        if (!vaultService.IsInsideVault(fullPath))
        {
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (File.Exists(fullPath))
        {
            return new { Error = $"Note already exists: {path}" };
        }

        string? directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        return new { Path = path, Created = true };
    }

    [McpServerTool, Description("Update an existing note in the Obsidian vault by overwriting its content")]
    public async Task<object> UpdateNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating note '{Path}'", path);

        string fullPath = vaultService.ResolveNotePath(path);

        if (!vaultService.IsInsideVault(fullPath))
        {
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        return new { Path = path, Updated = true };
    }

    [McpServerTool, Description("Delete a note from the Obsidian vault")]
    public Task<object> DeleteNote(string path)
    {
        logger.LogInformation("Deleting note '{Path}'", path);

        string fullPath = vaultService.ResolveNotePath(path);

        if (!vaultService.IsInsideVault(fullPath))
        {
            return Task.FromResult<object>(new { Error = "Path resolves outside the vault boundary" });
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<object>(new { Error = $"Note not found: {path}" });
        }

        File.Delete(fullPath);

        return Task.FromResult<object>(new { Path = path, Deleted = true });
    }

    [McpServerTool, Description("Append content to an existing note in the Obsidian vault")]
    public async Task<object> AppendToNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to note '{Path}'", path);

        string fullPath = vaultService.ResolveNotePath(path);

        if (!vaultService.IsInsideVault(fullPath))
        {
            return new { Error = "Path resolves outside the vault boundary" };
        }

        if (!File.Exists(fullPath))
        {
            return new { Error = $"Note not found: {path}" };
        }

        await File.AppendAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        return new { Path = path, Appended = true };
    }

    [McpServerTool, Description("List markdown notes in the Obsidian vault, optionally scoped to a subfolder")]
    public Task<object> ListNotes(
        string? folder = null,
        int limit = 50)
    {
        logger.LogInformation("Listing notes, folder '{Folder}'", folder);

        string vaultPath = vaultService.GetVaultPath();
        string searchPath = vaultPath;

        if (folder is not null)
        {
            searchPath = Path.GetFullPath(Path.Combine(vaultPath, folder));

            if (!vaultService.IsInsideVault(searchPath))
            {
                return Task.FromResult<object>(new { Error = "Folder resolves outside the vault boundary" });
            }
        }

        if (!Directory.Exists(searchPath))
        {
            return Task.FromResult<object>(new { Error = $"Directory not found: {folder ?? searchPath}" });
        }

        List<object> notes = Directory
            .EnumerateFiles(searchPath, "*.md", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(limit)
            .Select(f => (object)new
            {
                Name = Path.GetRelativePath(searchPath, f.FullName),
                LastModified = f.LastWriteTimeUtc
            })
            .ToList();

        return Task.FromResult<object>(new { Folder = folder, Count = notes.Count, Notes = notes });
    }
}
