using System.Diagnostics;
using Spectre.Console;

namespace ObsidianAgent.Cli;

public sealed class McpServerManager : IAsyncDisposable
{
    private Process? _serverProcess;
    private bool _cliStartedServer;

    public bool CliStartedServer => _cliStartedServer;

    public async Task<bool> EnsureRunningAsync(string endpoint, string projectPath, bool verbose)
    {
        bool running = await IsReachableAsync(endpoint).ConfigureAwait(false);

        if (running)
        {
            AnsiConsole.MarkupLine("[green]MCP server already running.[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[dim]MCP server not detected. Starting it automatically...[/]");

        _serverProcess = StartProcess(projectPath, verbose);

        if (_serverProcess is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to start MCP server.[/]");
            return false;
        }

        _cliStartedServer = true;

        bool healthy = await WaitForHealthyAsync(endpoint).ConfigureAwait(false);

        if (!healthy)
        {
            AnsiConsole.MarkupLine("[red]Timed out waiting for MCP server to start.[/]");
            await StopAsync().ConfigureAwait(false);
            return false;
        }

        AnsiConsole.MarkupLine("[green]MCP server is running.[/]");
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cliStartedServer && _serverProcess is not null)
        {
            AnsiConsole.MarkupLine("[dim]Stopping MCP server...[/]");
            await StopAsync().ConfigureAwait(false);
            AnsiConsole.MarkupLine("[green]MCP server stopped.[/]");
        }
    }

    private static async Task<bool> IsReachableAsync(string endpoint)
    {
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
            await client.GetAsync(new Uri(endpoint)).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Process? StartProcess(string projectPath, bool verbose)
    {
        string separator = verbose ? " -- --verbose" : "";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"{separator}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return Process.Start(startInfo);
    }

    private static async Task<bool> WaitForHealthyAsync(string endpoint)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync("Starting MCP server...", async ctx =>
            {
                using CancellationTokenSource timeoutCts = new(TimeSpan.FromMinutes(2));

                while (!timeoutCts.Token.IsCancellationRequested)
                {
                    bool reachable = await IsReachableAsync(endpoint).ConfigureAwait(false);

                    if (reachable)
                    {
                        return true;
                    }

                    ctx.Status("Waiting for MCP server to become healthy...");

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), timeoutCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                return false;
            }).ConfigureAwait(false);
    }

    private async Task StopAsync()
    {
        if (_serverProcess is null) return;

        try
        {
            if (!_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
                await _serverProcess.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        finally
        {
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }
}
