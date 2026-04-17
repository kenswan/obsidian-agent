using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class TemplateTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<TemplateTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("List available note templates")]
    public async Task<object> ListTemplates(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing templates");

        CliResult result = await obsidianCli.ListTemplatesAsync(cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Enrich(new { Count = lines.Length, Templates = lines }, result);
    }

    [McpServerTool, Description("Read the content of a note template")]
    public async Task<object> ReadTemplate(
        [Description("Template name")] string name,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading template '{Name}'", name);

        CliResult result = await obsidianCli.ReadTemplateAsync(name, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Name = name, Content = result.Output }, result);
    }

    [McpServerTool, Description("Create a new note from a template")]
    public async Task<object> CreateFromTemplate(
        [Description("File path for the new note")] string path,
        [Description("Template name to use")] string templateName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}' from template '{Template}'", path, templateName);

        CliResult result = await obsidianCli.CreateNoteFromTemplateAsync(path, templateName, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Template = templateName, Created = true }, result);
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
