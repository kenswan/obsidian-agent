using System.Diagnostics;

namespace ObsidianAgent.Mcp.Services;

public sealed class CliRunner(ILogger<CliRunner> logger) : ICliRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public async Task<CliResult> RunAsync(
        string command,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing: {Command} {Arguments}", command, arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        process.Start();

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);

        string output = await outputTask.ConfigureAwait(false);
        string error = await errorTask.ConfigureAwait(false);

        logger.LogInformation(
            "Command '{Command}' exited with code {ExitCode}",
            command,
            process.ExitCode);

        return new CliResult(process.ExitCode, output.Trim(), error.Trim())
        {
            ExecutedCommand = command,
            ExecutedArguments = arguments
        };
    }
}
