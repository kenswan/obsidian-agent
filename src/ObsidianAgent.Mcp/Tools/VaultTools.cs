using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class VaultTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<VaultTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("Get the heading structure (outline) of a note")]
    public async Task<object> GetOutline(
        [Description("File path in the vault")] string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting outline for '{Path}'", path);

        CliResult result = await obsidianCli.GetOutlineAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Outline = result.Output }, result);
    }

    [McpServerTool, Description("List all bookmarks in the vault")]
    public async Task<object> ListBookmarks(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing bookmarks");

        CliResult result = await obsidianCli.ListBookmarksAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Bookmarks = result.Output }, result);
    }

    [McpServerTool, Description("Add a bookmark for a file in the vault")]
    public async Task<object> AddBookmark(
        [Description("File path to bookmark")] string path,
        [Description("Optional bookmark title")] string? title = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding bookmark for '{Path}'", path);

        CliResult result = await obsidianCli.AddBookmarkAsync(path, title, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Bookmarked = true }, result);
    }

    [McpServerTool, Description("List recently opened files in the vault")]
    public async Task<object> ListRecents(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing recent files");

        CliResult result = await obsidianCli.ListRecentsAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Enrich(new { Count = lines.Length, Recents = lines }, result);
    }

    [McpServerTool, Description("Get vault information including name, path, file count, and size")]
    public async Task<object> GetVaultInfo(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting vault info");

        CliResult result = await obsidianCli.GetVaultInfoAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Info = result.Output }, result);
    }

    private object Enrich(object response, CliResult cliResult)
    {
        if (!config.Verbose) return response;

        return new
        {
            Result = response,
            Diagnostics = new
            {
                cliResult.ExecutedCommand,
                cliResult.ExecutedArguments,
                cliResult.ExitCode
            }
        };
    }
}
