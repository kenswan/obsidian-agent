using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class PropertyTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<PropertyTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("List all frontmatter properties used in the vault or in a specific file, with occurrence counts")]
    public async Task<object> ListProperties(
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing properties, path='{Path}'", path);

        CliResult result = await obsidianCli.ListPropertiesAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Properties = result.Output }, result);
    }

    [McpServerTool, Description("Read a frontmatter property value from a specific file")]
    public async Task<object> ReadProperty(
        [Description("Property name")] string name,
        [Description("File path in the vault")] string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading property '{Name}' from '{Path}'", name, path);

        CliResult result = await obsidianCli.ReadPropertyAsync(name, path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Name = name, Path = path, Value = result.Output }, result);
    }

    [McpServerTool, Description("Set a frontmatter property on a file. Valid types: text, list, number, checkbox, date, datetime")]
    public async Task<object> SetProperty(
        [Description("Property name")] string name,
        [Description("Property value")] string value,
        [Description("File path in the vault")] string path,
        [Description("Property type (text, list, number, checkbox, date, datetime). Defaults to text.")] string? type = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting property '{Name}' on '{Path}'", name, path);

        CliResult result = await obsidianCli.SetPropertyAsync(name, value, path, type, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Name = name, Path = path, Value = value, Set = true }, result);
    }

    [McpServerTool, Description("Remove a frontmatter property from a file")]
    public async Task<object> RemoveProperty(
        [Description("Property name")] string name,
        [Description("File path in the vault")] string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing property '{Name}' from '{Path}'", name, path);

        CliResult result = await obsidianCli.RemovePropertyAsync(name, path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Name = name, Path = path, Removed = true }, result);
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
