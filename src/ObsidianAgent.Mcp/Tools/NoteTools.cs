using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using ObsidianAgent.Mcp.Configuration;
using ObsidianAgent.Mcp.Services;

namespace ObsidianAgent.Mcp.Tools;

[McpServerToolType]
public class NoteTools(
    IObsidianCliService obsidianCli,
    IOptions<ObsidianOptions> options,
    ILogger<NoteTools> logger)
{
    private readonly ObsidianOptions config = options.Value;

    [McpServerTool, Description("Read the content of a note from the Obsidian vault")]
    public async Task<object> ReadNote(
        string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reading note '{Path}'", path);

        CliResult result = await obsidianCli.ReadNoteAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Content = result.Output }, result);
    }

    [McpServerTool, Description("Create a new note in the Obsidian vault. Fails if the note already exists.")]
    public async Task<object> CreateNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating note '{Path}'", path);

        CliResult result = await obsidianCli.CreateNoteAsync(path, content, overwrite: false, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Created = true }, result);
    }

    [McpServerTool, Description("Update an existing note in the Obsidian vault by overwriting its content")]
    public async Task<object> UpdateNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating note '{Path}'", path);

        CliResult result = await obsidianCli.CreateNoteAsync(path, content, overwrite: true, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Updated = true }, result);
    }

    [McpServerTool, Description("Delete a note from the Obsidian vault")]
    public async Task<object> DeleteNote(
        string path,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting note '{Path}'", path);

        CliResult result = await obsidianCli.DeleteNoteAsync(path, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Deleted = true }, result);
    }

    [McpServerTool, Description("Append content to an existing note in the Obsidian vault")]
    public async Task<object> AppendToNote(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Appending to note '{Path}'", path);

        CliResult result = await obsidianCli.AppendToNoteAsync(path, content, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        return Enrich(new { Path = path, Appended = true }, result);
    }

    [McpServerTool, Description("List markdown notes in the Obsidian vault, optionally scoped to a subfolder")]
    public async Task<object> ListNotes(
        string? folder = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing notes, folder '{Folder}'", folder);

        CliResult result = await obsidianCli.ListFilesAsync(folder, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Enrich(new { Error = result.Error.Length > 0 ? result.Error : result.Output }, result);
        }

        string[] lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<object> notes = lines
            .Take(limit)
            .Select(line => (object)new { Name = line })
            .ToList();

        return Enrich(new { Folder = folder, Count = notes.Count, Notes = notes }, result);
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
