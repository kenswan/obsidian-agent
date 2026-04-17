using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class DailyNoteTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<DailyNoteTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("Read the content of today's daily note")]
    public async Task<object> ReadDailyNote(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading daily note");

        CliResult result = await obsidianCli.ReadDailyNoteAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Content = result.Output }, result);
    }

    [McpServerTool, Description("Append content to today's daily note")]
    public async Task<object> AppendToDailyNote(
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to daily note");

        CliResult result = await obsidianCli.AppendToDailyNoteAsync(content, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Appended = true }, result);
    }

    [McpServerTool, Description("Prepend content to today's daily note")]
    public async Task<object> PrependToDailyNote(
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Prepending to daily note");

        CliResult result = await obsidianCli.PrependToDailyNoteAsync(content, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Prepended = true }, result);
    }

    [McpServerTool, Description("Get the file path of today's daily note")]
    public async Task<object> GetDailyNotePath(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting daily note path");

        CliResult result = await obsidianCli.GetDailyNotePathAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = result.Output }, result);
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
