namespace ObsidianAgent.Mcp.Services;

public interface IObsidianCliService
{
    Task<CliResult> ReadNoteAsync(string path, CancellationToken cancellationToken = default);

    Task<CliResult> CreateNoteAsync(string path, string content, bool overwrite = false, CancellationToken cancellationToken = default);

    Task<CliResult> DeleteNoteAsync(string path, CancellationToken cancellationToken = default);

    Task<CliResult> AppendToNoteAsync(string path, string content, CancellationToken cancellationToken = default);

    Task<CliResult> ListFilesAsync(string? folder = null, int? limit = null, CancellationToken cancellationToken = default);

    Task<CliResult> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
}
