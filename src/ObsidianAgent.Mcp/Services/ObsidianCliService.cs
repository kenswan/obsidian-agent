using System.Text;
using Microsoft.Extensions.Options;
using ObsidianAgent.Mcp.Configuration;

namespace ObsidianAgent.Mcp.Services;

public sealed class ObsidianCliService(
    ICliRunner cliRunner,
    IOptions<ObsidianOptions> options,
    ILogger<ObsidianCliService> logger) : IObsidianCliService
{
    private readonly ObsidianOptions config = options.Value;

    // --- Notes ---

    public Task<CliResult> ReadNoteAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading note '{Path}'", path);
        string args = BuildArgs("read", $"path=\"{path}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> CreateNoteAsync(string path, string content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}', overwrite={Overwrite}", path, overwrite);
        string overwriteFlag = overwrite ? " overwrite" : "";
        string args = BuildArgs("create", $"path=\"{path}\" content=\"{EscapeContent(content)}\"{overwriteFlag}");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> DeleteNoteAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting note '{Path}'", path);
        string args = BuildArgs("delete", $"path=\"{path}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> AppendToNoteAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to note '{Path}'", path);
        string args = BuildArgs("append", $"path=\"{path}\" content=\"{EscapeContent(content)}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> ListFilesAsync(string? folder = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing files, folder='{Folder}'", folder);
        string extra = "ext=md";
        if (folder is not null)
            extra += $" folder=\"{folder}\"";
        string args = BuildArgs("files", extra);
        return Run(args, cancellationToken);
    }

    // --- Search ---

    public Task<CliResult> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for '{Query}'", query);
        string args = BuildArgs("search", $"query=\"{EscapeContent(query)}\" limit={limit}");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> SearchWithContextAsync(string query, string? path = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching with context for '{Query}'", query);
        var extra = $"query=\"{EscapeContent(query)}\" limit={limit} format=json";
        if (path is not null)
            extra += $" path=\"{path}\"";
        string args = BuildArgs("search:context", extra);
        return Run(args, cancellationToken);
    }

    // --- Daily Notes ---

    public Task<CliResult> ReadDailyNoteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading daily note");
        string args = BuildArgs("daily:read", "");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> AppendToDailyNoteAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to daily note");
        string args = BuildArgs("daily:append", $"content=\"{EscapeContent(content)}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> PrependToDailyNoteAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Prepending to daily note");
        string args = BuildArgs("daily:prepend", $"content=\"{EscapeContent(content)}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> GetDailyNotePathAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting daily note path");
        string args = BuildArgs("daily:path", "");
        return Run(args, cancellationToken);
    }

    // --- Properties ---

    public Task<CliResult> ListPropertiesAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing properties, path='{Path}'", path);
        var extra = "format=json counts";
        if (path is not null)
            extra += $" path=\"{path}\"";
        string args = BuildArgs("properties", extra);
        return Run(args, cancellationToken);
    }

    public Task<CliResult> ReadPropertyAsync(string name, string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading property '{Name}' from '{Path}'", name, path);
        string args = BuildArgs("property:read", $"name=\"{name}\" path=\"{path}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> SetPropertyAsync(string name, string value, string path, string? type = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting property '{Name}' on '{Path}'", name, path);
        var extra = $"name=\"{name}\" value=\"{EscapeContent(value)}\" path=\"{path}\"";
        if (type is not null)
            extra += $" type={type}";
        string args = BuildArgs("property:set", extra);
        return Run(args, cancellationToken);
    }

    public Task<CliResult> RemovePropertyAsync(string name, string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing property '{Name}' from '{Path}'", name, path);
        string args = BuildArgs("property:remove", $"name=\"{name}\" path=\"{path}\"");
        return Run(args, cancellationToken);
    }

    // --- Tags ---

    public Task<CliResult> ListTagsAsync(string? path = null, bool sortByCount = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing tags, path='{Path}'", path);
        var extra = "format=json counts";
        if (sortByCount)
            extra += " sort=count";
        if (path is not null)
            extra += $" path=\"{path}\"";
        string args = BuildArgs("tags", extra);
        return Run(args, cancellationToken);
    }

    public Task<CliResult> GetTagAsync(string name, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting tag info for '{Name}'", name);
        string args = BuildArgs("tag", $"name=\"{name}\" verbose");
        return Run(args, cancellationToken);
    }

    // --- Graph ---

    public Task<CliResult> GetBacklinksAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting backlinks for '{Path}'", path);
        string args = BuildArgs("backlinks", $"path=\"{path}\" counts format=json");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> GetLinksAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting links from '{Path}'", path);
        string args = BuildArgs("links", $"path=\"{path}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> FindOrphansAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding orphan notes");
        string args = BuildArgs("orphans", "");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> FindDeadEndsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding dead-end notes");
        string args = BuildArgs("deadends", "");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> FindUnresolvedAsync(bool includeSources = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding unresolved links");
        var extra = "counts format=json";
        if (includeSources)
            extra += " verbose";
        string args = BuildArgs("unresolved", extra);
        return Run(args, cancellationToken);
    }

    // --- Tasks ---

    public Task<CliResult> ListTasksAsync(string? path = null, bool? showDone = null, bool? showTodo = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing tasks, path='{Path}'", path);
        var extra = "format=json verbose";
        if (showDone == true)
            extra += " done";
        if (showTodo == true)
            extra += " todo";
        if (path is not null)
            extra += $" path=\"{path}\"";
        string args = BuildArgs("tasks", extra);
        return Run(args, cancellationToken);
    }

    public Task<CliResult> UpdateTaskAsync(string path, int line, string action, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating task at '{Path}' line {Line} with action '{Action}'", path, line, action);
        string args = BuildArgs("task", $"path=\"{path}\" line={line} {action}");
        return Run(args, cancellationToken);
    }

    // --- Templates ---

    public Task<CliResult> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing templates");
        string args = BuildArgs("templates", "");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> ReadTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading template '{Name}'", name);
        string args = BuildArgs("template:read", $"name=\"{name}\"");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> CreateNoteFromTemplateAsync(string path, string templateName, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}' from template '{Template}'", path, templateName);
        string args = BuildArgs("create", $"path=\"{path}\" template=\"{templateName}\"");
        return Run(args, cancellationToken);
    }

    // --- Vault ---

    public Task<CliResult> GetOutlineAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting outline for '{Path}'", path);
        string args = BuildArgs("outline", $"path=\"{path}\" format=json");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> ListBookmarksAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing bookmarks");
        string args = BuildArgs("bookmarks", "format=json verbose");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> AddBookmarkAsync(string path, string? title = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding bookmark for '{Path}'", path);
        var extra = $"file=\"{path}\"";
        if (title is not null)
            extra += $" title=\"{EscapeContent(title)}\"";
        string args = BuildArgs("bookmark", extra);
        return Run(args, cancellationToken);
    }

    public Task<CliResult> ListRecentsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing recent files");
        string args = BuildArgs("recents", "");
        return Run(args, cancellationToken);
    }

    public Task<CliResult> GetVaultInfoAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting vault info");
        string args = BuildArgs("vault", "");
        return Run(args, cancellationToken);
    }

    // --- Helpers ---

    private Task<CliResult> Run(string args, CancellationToken cancellationToken)
    {
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    private string BuildArgs(string command, string commandArgs)
    {
        return string.IsNullOrWhiteSpace(commandArgs)
            ? $"{command} vault={config.VaultName}"
            : $"{command} vault={config.VaultName} {commandArgs}";
    }

    private static string EscapeContent(string content)
    {
        return content
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
