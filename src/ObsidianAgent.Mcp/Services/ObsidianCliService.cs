using Microsoft.Extensions.Options;
using ObsidianAgent.Mcp.Configuration;

namespace ObsidianAgent.Mcp.Services;

public sealed class ObsidianCliService(
    ICliRunner cliRunner,
    IOptions<ObsidianOptions> options,
    ILogger<ObsidianCliService> logger) : IObsidianCliService
{
    private readonly ObsidianOptions config = options.Value;

    public Task<CliResult> ReadNoteAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading note '{Path}'", path);
        string args = BuildArgs("read", $"path=\"{path}\"");
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    public Task<CliResult> CreateNoteAsync(string path, string content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}', overwrite={Overwrite}", path, overwrite);
        string overwriteFlag = overwrite ? " overwrite" : "";
        string args = BuildArgs("create", $"path=\"{path}\" content=\"{EscapeContent(content)}\"{overwriteFlag}");
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    public Task<CliResult> DeleteNoteAsync(string path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting note '{Path}'", path);
        string args = BuildArgs("delete", $"path=\"{path}\"");
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    public Task<CliResult> AppendToNoteAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to note '{Path}'", path);
        string args = BuildArgs("append", $"path=\"{path}\" content=\"{EscapeContent(content)}\"");
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    public Task<CliResult> ListFilesAsync(string? folder = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing files, folder='{Folder}'", folder);
        string extra = "ext=md";
        if (folder is not null)
        {
            extra += $" folder=\"{folder}\"";
        }
        string args = BuildArgs("files", extra);
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    public Task<CliResult> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for '{Query}'", query);
        string args = BuildArgs("search", $"query=\"{EscapeContent(query)}\" limit={limit}");
        return cliRunner.RunAsync(config.CliCommand, args, cancellationToken: cancellationToken);
    }

    private string BuildArgs(string command, string commandArgs)
    {
        return $"{command} vault={config.VaultName} {commandArgs}";
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
