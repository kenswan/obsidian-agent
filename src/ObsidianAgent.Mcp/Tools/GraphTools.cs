using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class GraphTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<GraphTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("List files that link to a given note (backlinks), with link counts")]
    public async Task<object> GetBacklinks(
        [Description("File path in the vault")] string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting backlinks for '{Path}'", path);

        CliResult result = await obsidianCli.GetBacklinksAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Backlinks = result.Output }, result);
    }

    [McpServerTool, Description("List outgoing links from a note")]
    public async Task<object> GetLinks(
        [Description("File path in the vault")] string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting links from '{Path}'", path);

        CliResult result = await obsidianCli.GetLinksAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Links = result.Output }, result);
    }

    [McpServerTool, Description("Find notes with no incoming links (orphans) — notes that nothing links to")]
    public async Task<object> FindOrphans(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding orphan notes");

        CliResult result = await obsidianCli.FindOrphansAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Enrich(new { Count = lines.Length, Orphans = lines }, result);
    }

    [McpServerTool, Description("Find notes with no outgoing links (dead ends) — notes that don't link to anything")]
    public async Task<object> FindDeadEnds(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding dead-end notes");

        CliResult result = await obsidianCli.FindDeadEndsAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Enrich(new { Count = lines.Length, DeadEnds = lines }, result);
    }

    [McpServerTool, Description("Find broken or unresolved wikilinks in the vault")]
    public async Task<object> FindUnresolved(
        [Description("Include source files that contain unresolved links")] bool includeSources = false,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Finding unresolved links");

        CliResult result = await obsidianCli.FindUnresolvedAsync(includeSources, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Unresolved = result.Output }, result);
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
