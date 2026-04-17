using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class TaskTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<TaskTools> logger)
{
    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase) { "toggle", "done", "todo" };

    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("List tasks across the vault or in a specific file. Can filter by completion status.")]
    public async Task<object> ListTasks(
        [Description("Optional file path to scope tasks to")] string? path = null,
        [Description("Show only completed tasks")] bool? showDone = null,
        [Description("Show only incomplete tasks")] bool? showTodo = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing tasks, path='{Path}'", path);

        CliResult result = await obsidianCli.ListTasksAsync(path, showDone, showTodo, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Tasks = result.Output }, result);
    }

    [McpServerTool, Description("Update a task's status. Action must be 'toggle', 'done', or 'todo'.")]
    public async Task<object> UpdateTask(
        [Description("File path containing the task")] string path,
        [Description("Line number of the task")] int line,
        [Description("Action to perform: toggle, done, or todo")] string action,
        CancellationToken cancellationToken = default)
    {
        if (!ValidActions.Contains(action))
        {
            return new { Error = $"Invalid action '{action}'. Must be one of: toggle, done, todo." };
        }

        logger.LogInformation("Updating task at '{Path}' line {Line} with action '{Action}'", path, line, action);

        CliResult result = await obsidianCli.UpdateTaskAsync(path, line, action, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Line = line, Action = action, Updated = true }, result);
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
