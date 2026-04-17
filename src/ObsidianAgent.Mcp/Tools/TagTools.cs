using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class TagTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<TagTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("List tags used in the vault or in a specific file, with occurrence counts")]
    public async Task<object> ListTags(
        [Description("Optional file path to scope tags to")] string? path = null,
        [Description("Sort by occurrence count instead of name")] bool sortByCount = false,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing tags, path='{Path}'", path);

        CliResult result = await obsidianCli.ListTagsAsync(path, sortByCount, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Tags = result.Output }, result);
    }

    [McpServerTool, Description("Get detailed info about a specific tag, including which files use it")]
    public async Task<object> GetTag(
        [Description("Tag name (without #)")] string name,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting tag info for '{Name}'", name);

        CliResult result = await obsidianCli.GetTagAsync(name, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Name = name, Info = result.Output }, result);
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
