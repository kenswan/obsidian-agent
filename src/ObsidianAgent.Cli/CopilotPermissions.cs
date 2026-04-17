using GitHub.Copilot.SDK;
using Spectre.Console;

namespace ObsidianAgent.Cli;

public static class CopilotPermissions
{
    public static Task<PermissionRequestResult> PromptAsync(
        PermissionRequest request,
        PermissionInvocation invocation)
    {
        AnsiConsole.WriteLine();

        bool approved = AnsiConsole.Confirm(
            $"[yellow]Copilot requests permission:[/] [cyan]{Markup.Escape(request.Kind ?? "unknown")}[/] — approve?",
            defaultValue: false);

        return Task.FromResult(new PermissionRequestResult
        {
            Kind = approved
                ? PermissionRequestResultKind.Approved
                : PermissionRequestResultKind.DeniedInteractivelyByUser
        });
    }
}
