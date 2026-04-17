using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class SearchTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<SearchTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("Search for notes containing the given text query")]
    public async Task<object> SearchNotes(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching notes for '{Query}'", query);

        CliResult result = await obsidianCli.SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(
                new { Query = query, Count = 0, Matches = Array.Empty<object>(), Error = result.Error.Length > 0 ? result.Error : result.Output },
                result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<object> matches = lines
            .Select(line => (object)new { Path = line })
            .ToList();

        return Enrich(new { Query = query, Count = matches.Count, Matches = matches }, result);
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
