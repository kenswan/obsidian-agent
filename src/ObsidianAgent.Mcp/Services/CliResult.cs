namespace ObsidianAgent.Mcp.Services;

public sealed record CliResult(int ExitCode, string Output, string Error)
{
    public bool IsSuccess => ExitCode == 0;
}
