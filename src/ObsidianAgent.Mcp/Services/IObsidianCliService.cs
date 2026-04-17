namespace ObsidianAgent.Mcp.Services;

public interface IObsidianCliService
{
    // Notes
    Task<CliResult> ReadNoteAsync(string path, CancellationToken cancellationToken = default);
    Task<CliResult> CreateNoteAsync(string path, string content, bool overwrite = false, CancellationToken cancellationToken = default);
    Task<CliResult> DeleteNoteAsync(string path, CancellationToken cancellationToken = default);
    Task<CliResult> AppendToNoteAsync(string path, string content, CancellationToken cancellationToken = default);
    Task<CliResult> ListFilesAsync(string? folder = null, int? limit = null, CancellationToken cancellationToken = default);

    // Search
    Task<CliResult> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
    Task<CliResult> SearchWithContextAsync(string query, string? path = null, int limit = 20, CancellationToken cancellationToken = default);

    // Daily Notes
    Task<CliResult> ReadDailyNoteAsync(CancellationToken cancellationToken = default);
    Task<CliResult> AppendToDailyNoteAsync(string content, CancellationToken cancellationToken = default);
    Task<CliResult> PrependToDailyNoteAsync(string content, CancellationToken cancellationToken = default);
    Task<CliResult> GetDailyNotePathAsync(CancellationToken cancellationToken = default);

    // Properties
    Task<CliResult> ListPropertiesAsync(string? path = null, CancellationToken cancellationToken = default);
    Task<CliResult> ReadPropertyAsync(string name, string path, CancellationToken cancellationToken = default);
    Task<CliResult> SetPropertyAsync(string name, string value, string path, string? type = null, CancellationToken cancellationToken = default);
    Task<CliResult> RemovePropertyAsync(string name, string path, CancellationToken cancellationToken = default);

    // Tags
    Task<CliResult> ListTagsAsync(string? path = null, bool sortByCount = false, CancellationToken cancellationToken = default);
    Task<CliResult> GetTagAsync(string name, CancellationToken cancellationToken = default);

    // Graph
    Task<CliResult> GetBacklinksAsync(string path, CancellationToken cancellationToken = default);
    Task<CliResult> GetLinksAsync(string path, CancellationToken cancellationToken = default);
    Task<CliResult> FindOrphansAsync(CancellationToken cancellationToken = default);
    Task<CliResult> FindDeadEndsAsync(CancellationToken cancellationToken = default);
    Task<CliResult> FindUnresolvedAsync(bool includeSources = false, CancellationToken cancellationToken = default);

    // Tasks
    Task<CliResult> ListTasksAsync(string? path = null, bool? showDone = null, bool? showTodo = null, CancellationToken cancellationToken = default);
    Task<CliResult> UpdateTaskAsync(string path, int line, string action, CancellationToken cancellationToken = default);

    // Templates
    Task<CliResult> ListTemplatesAsync(CancellationToken cancellationToken = default);
    Task<CliResult> ReadTemplateAsync(string name, CancellationToken cancellationToken = default);
    Task<CliResult> CreateNoteFromTemplateAsync(string path, string templateName, CancellationToken cancellationToken = default);

    // Vault
    Task<CliResult> GetOutlineAsync(string path, CancellationToken cancellationToken = default);
    Task<CliResult> ListBookmarksAsync(CancellationToken cancellationToken = default);
    Task<CliResult> AddBookmarkAsync(string path, string? title = null, CancellationToken cancellationToken = default);
    Task<CliResult> ListRecentsAsync(CancellationToken cancellationToken = default);
    Task<CliResult> GetVaultInfoAsync(CancellationToken cancellationToken = default);
}
