namespace ObsidianAgent.Mcp.Services;

public interface ICliRunner
{
    Task<CliResult> RunAsync(
        string command,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}
